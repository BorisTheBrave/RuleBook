using System.Data;

namespace RuleBook
{
    #region RuleResult
    public interface IRuleResult
    {
        bool TryGetReturnValue<TValue>(out TValue value)
        {
            value = default;
            return false;
        }

    }

    public class ContinueRuleResult : IRuleResult
    {
        public static ContinueRuleResult Instance = new ContinueRuleResult();

        private ContinueRuleResult() { }
    }


    public class ReturnRuleResult<T> : IRuleResult
    {
        public ReturnRuleResult(T value)
        {
            Value = value;
        }

        public T Value { get; private set; }

        public bool TryGetReturnValue<TValue>(out TValue value)
        {
            if(typeof(T).IsAssignableTo(typeof(TValue)))
            {
                value = (TValue)(object)Value;
                return true;

            }
            value = default;
            return false;
        }

        public override bool Equals(object? obj)
        {
            return obj is ReturnRuleResult<T> result &&
                   EqualityComparer<T>.Default.Equals(Value, result.Value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }
    }

    // TOOD: Stop/AnyStop for Return<void>

    public class ChangeArgsRuleResult<TArgs> : IRuleResult
    {
        public ChangeArgsRuleResult(TArgs newArgs)
        {
            NewArgs = newArgs;
        }

        public TArgs NewArgs { get; private set; }
    }

    public static class RuleResult
    {
        public static ContinueRuleResult Continue => ContinueRuleResult.Instance;
        public static ReturnRuleResult<T> Return<T>(T value) => new ReturnRuleResult<T>(value);
        public static ChangeArgsRuleResult<ValueTuple<T1>> ChangeArgs<T1>(T1 arg1) => new ChangeArgsRuleResult<ValueTuple<T1>>(new ValueTuple<T1>(arg1));
    }
    #endregion

    public static class Phase
    {
        // Optional - these are the inform 7 ones, but any set works
        public const int Before = -3;
        public const int Instead = -2;
        public const int Check = -1;
        public const int CarryOut = 0;
        public const int After = 1;
        public const int Report = 2;
    }

    // TODO Variants for different args, void returns, coroutines, accumulating


    /// <summary>
    /// Represents a single rule that can be inserted into a FuncBook.
    /// Rules can only belong to a single rulebook.
    /// </summary>
    public class FuncRule<TArg, TRet> : IComparable<FuncRule<TArg, TRet>>
    {
        private FuncBook<TArg, TRet> parent;
        private string name;
        private int order;
        private Func<TArg, bool> condition;
        // At most one of the following should be defined
        private Func<TArg, IRuleResult>? funcBody;
        private Func<Func<TArg, IRuleResult>, TArg, IRuleResult>? wrapBody;
        private FuncBook<TArg, TRet>? bookBody;


        public FuncBook<TArg, TRet> Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent?.RemoveRule(this);
                parent = value;
                parent?.AddRule(this);
            }
        }
        public string Name { get { return name; } set { name = value; } }
        public int Order { get { return order; } set { order = value; Reorder(); } }
        public Func<TArg, bool> Condition { get { return condition; } set { condition = value; Reorder(); } }
        // At most one of the following should be defined
        public Func<TArg, IRuleResult>? FuncBody
        {
            get { return funcBody; }
            set
            {
                if (value != null && (wrapBody != null || bookBody != null)) RaiseBodyError();
                funcBody = value;
            }
        }
        public Func<Func<TArg, IRuleResult>, TArg, IRuleResult>? WrapBody
        {
            get { return wrapBody; }
            set
            {
                if (value != null && (funcBody != null || bookBody != null)) RaiseBodyError();
                wrapBody = value;
            }
        }
        public FuncBook<TArg, TRet>? BookBody
        {
            get { return bookBody; }
            set
            {
                if (value != null && (funcBody != null || wrapBody != null)) RaiseBodyError();
                bookBody = value;
            }
        }

        public int CompareTo(FuncRule<TArg, TRet>? other)
        {
            if (this.Order < other.Order) return -1;
            if (this.Order > other.Order) return 1;
            if (this.Condition != null && other.Condition == null) return -1;
            if (this.Condition == null && other.Condition != null) return 1;
            return 0;
        }

        private void Reorder()
        {
            parent?.ReorderRule(this);
        }

        private void RaiseBodyError()
        {
            throw new Exception($"A rule can only have one of FuncBody, WrapBody or BookBody set. Unset the others first");
        }
    }

    /// <summary>
    /// A collection of rules, that collectively define a callable function with signature Func<TArg, TRet>
    /// </summary>
    public class FuncBook<TArg, TRet>
    {
        public class RuleBuilder
        {
            private FuncBook<TArg, TRet> parent;

            private string name;
            private int order;
            private Func<TArg, bool> pre;
            private Func<TArg, IRuleResult> body;
            public Func<Func<TArg, IRuleResult>, TArg, IRuleResult> wrapBody;

            internal RuleBuilder(FuncBook<TArg, TRet> parent)
            {
                this.parent = parent;
            }

            public RuleBuilder When(Func<TArg, bool> pre)
            {
                this.pre = pre;
                return this;
            }

            public RuleBuilder Named(string name)
            {
                this.name = name;
                return this;
            }

            public RuleBuilder At(int order)
            {
                this.order = order;
                return this;
            }

            public FuncRule<TArg, TRet> Instead(TRet value)
            {
                this.body = (arg1) => RuleResult.Return(value);
                return Finish();
            }

            public FuncRule<TArg, TRet> Instead(Func<TArg, TRet> body)
            {
                this.body = (arg1) => RuleResult.Return(body(arg1));
                return Finish();
            }


            public FuncRule<TArg, TRet> Do(Action<TArg> body)
            {
                this.body = (arg) => { body(arg); return RuleResult.Continue; };
                return Finish();
            }

            public FuncRule<TArg, TRet> WithBody(Func<TArg, IRuleResult> body)
            {
                this.body = body;
                return Finish();
            }

            public FuncRule<TArg, TRet> WrapBody(Func<Func<TArg, IRuleResult>, TArg, IRuleResult> wrapBody)
            {
                this.wrapBody = wrapBody;
                return Finish();
            }

            private FuncRule<TArg, TRet> Finish()
            {
                var rule = new FuncRule<TArg, TRet>
                {
                    Name = name,
                    Order = order,
                    Condition = pre,
                    FuncBody = body,
                    WrapBody = wrapBody,
                    BookBody = null,
                };
                parent.AddRule(rule);
                return rule;
            }
        }


        private List<FuncRule<TArg, TRet>> rules = new List<FuncRule<TArg, TRet>>();

        public FuncBook() 
        {
        }

        public RuleBuilder AddRule()
        {
            return new RuleBuilder(this);
        }

        public void AddRule(FuncRule<TArg, TRet> rule)
        {
            if (rule.Parent == this)
            {
                rules.Add(rule);
                ReorderRule(rule);
            }
            else
            {
                rule.Parent = this;
            }
        }

        public void RemoveRule(FuncRule<TArg, TRet> rule)
        {
            if (rule.Parent == this)
            {
                rules.Remove(rule);
            }
            else
            {
                rule.Parent = null;
            }
        }

        internal void ReorderRule(FuncRule<TArg, TRet> rule)
        {
            // List.Sort() not stable, sadly
            rules = rules.OrderBy(x => x).ToList();
        }

        public TRet Invoke(TArg arg)
        {
            var ruleResult = Evaluate(arg);
            //TODO: Method for this?
            switch(ruleResult)
            {
                case ReturnRuleResult<TRet> r:
                    return r.Value;
                case null:
                case ContinueRuleResult _:
                case ChangeArgsRuleResult<ValueTuple<TArg>> _:
                    throw new Exception("No rule produced a result");
                default:
                    // TODO: Give a better explanation. Perhaps you got the types wrong?
                    throw new Exception("Unexpected rule result");

            }
        }

        public IRuleResult Evaluate(TArg arg) => Evaluate(arg, 0);

        private IRuleResult Evaluate(TArg arg1, int startingAt)
        {
            // TODO: Protect against mutation while this is running?
            for (var i = startingAt; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule.Condition != null && !rule.Condition(arg1))
                {
                    continue;
                }
                IRuleResult ruleResult;
                if (rule.FuncBody != null)
                {
                    ruleResult = rule.FuncBody(arg1);
                }
                else if (rule.WrapBody != null)
                {
                    // Tail call, as we're doing the rest of the evaluation
                    // in the first method.
                    return rule.WrapBody(arg1 => Evaluate(arg1, startingAt + 1), arg1);
                }
                else if (rule.BookBody != null)
                {
                    ruleResult = rule.BookBody.Evaluate(arg1);
                }
                else
                {
                    ruleResult = RuleResult.Continue;
                }
                switch (ruleResult)
                {
                    case ReturnRuleResult<TRet> r:
                        return r;
                    case null:
                    case ContinueRuleResult _:
                        continue;
                    case ChangeArgsRuleResult<ValueTuple<TArg>> c:
                        arg1 = c.NewArgs.Item1;
                        continue;
                    default:
                        // TODO: Give a better explanation. Perhaps you got the types wrong?
                        throw new Exception("Unexpected rule result");
                }
            }
            return RuleResult.Continue;
        }

    }
}
