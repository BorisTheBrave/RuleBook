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

    public class FuncBook<TArg, TRet>
    {
        private class Rule : IComparable<Rule>
        {
            public string Name { get; init; }
            public int Order { get; init; }
            public Func<TArg, bool> Pre { get; init; }
            // At most one of the following should be defined
            public Func<TArg, IRuleResult>? Body { get; init; }
            public Func<Func<TArg, IRuleResult>, TArg, IRuleResult>? WrapBody { get; init; }
            public FuncBook<TArg, TRet>? BookBody { get; init; }

            public int CompareTo(FuncBook<TArg, TRet>.Rule? other)
            {
                if(this.Order < other.Order) return -1;
                if(this.Order > other.Order) return 1;
                if (this.Pre != null && other.Pre == null) return -1;
                if (this.Pre == null && other.Pre != null) return 1;
                return 0;
            }
        }

        public class RuleBuilder
        {
            public FuncBook<TArg, TRet> parent;

            private string name;
            private int phase;
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

            public void Instead(TRet value)
            {
                this.body = (arg1) => RuleResult.Return(value);
                Finish();
            }

            public void Instead(Func<TArg, TRet> body)
            {
                this.body = (arg1) => RuleResult.Return(body(arg1));
                Finish();
            }


            public void Do(Action<TArg> body)
            {
                this.body = (arg) => { body(arg); return RuleResult.Continue; };
                Finish();
            }

            public void WithBody(Func<TArg, IRuleResult> body)
            {
                this.body = body;
                Finish();
            }

            public void WrapBody(Func<Func<TArg, IRuleResult>, TArg, IRuleResult> wrapBody)
            {
                this.wrapBody = wrapBody;
                Finish();
            }

            private void Finish()
            {
                var rule = new Rule
                {
                    Name = name,
                    Order = order,
                    Pre = pre,
                    Body = body,
                    WrapBody = wrapBody,
                    BookBody = null,
                };
                parent.rules.Add(rule);
                // List.Sort() not stable, sadly
                parent.rules = parent.rules.OrderBy(x => x).ToList();
            }
        }


        private List<Rule> rules = new List<Rule>();

        public FuncBook() 
        {
        }

        public RuleBuilder AddRule()
        {
            return new RuleBuilder(this);
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
                if (rule.Pre != null && !rule.Pre(arg1))
                {
                    continue;
                }
                IRuleResult ruleResult;
                if (rule.Body != null)
                {
                    ruleResult = rule.Body(arg1);
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
