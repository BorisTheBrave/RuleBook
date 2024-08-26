using System.Data;

namespace RuleBook
{
    /// <summary>
    /// A collection of rules, that collectively define a callable function with signature Func<TArg1, TRet>
    /// </summary>
    public class FuncBook<TArg1, TRet>
    {
        public class RuleBuilder
        {
            private FuncBook<TArg1, TRet> parent;

            private string name;
            private int order;
            private Func<TArg1, bool> pre;
            private Func<TArg1, IRuleResult> body;
            public Func<Func<TArg1, IRuleResult>, TArg1, IRuleResult> wrapBody;

            internal RuleBuilder(FuncBook<TArg1, TRet> parent)
            {
                this.parent = parent;
            }

            public RuleBuilder When(Func<TArg1, bool> pre)
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

            public FuncRule<TArg1, TRet> Instead(TRet value)
            {
                this.body = (arg1) => RuleResult.Return(value);
                return Finish();
            }

            public FuncRule<TArg1, TRet> Instead(Func<TArg1, TRet> body)
            {
                this.body = (arg1) => RuleResult.Return(body(arg1));
                return Finish();
            }


            public FuncRule<TArg1, TRet> Do(Action<TArg1> body)
            {
                this.body = (arg1) => { body(arg1); return RuleResult.Continue; };
                return Finish();
            }

            public FuncRule<TArg1, TRet> WithBody(Func<TArg1, IRuleResult> body)
            {
                this.body = body;
                return Finish();
            }

            public FuncRule<TArg1, TRet> WrapBody(Func<Func<TArg1, IRuleResult>, TArg1, IRuleResult> wrapBody)
            {
                this.wrapBody = wrapBody;
                return Finish();
            }

            private FuncRule<TArg1, TRet> Finish()
            {
                var rule = new FuncRule<TArg1, TRet>
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


        private List<FuncRule<TArg1, TRet>> rules = new List<FuncRule<TArg1, TRet>>();

        public FuncBook() 
        {
        }

        public RuleBuilder AddRule()
        {
            return new RuleBuilder(this);
        }

        public void AddRule(FuncRule<TArg1, TRet> rule)
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

        public void RemoveRule(FuncRule<TArg1, TRet> rule)
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

        internal void ReorderRule(FuncRule<TArg1, TRet> rule)
        {
            // List.Sort() not stable, sadly
            rules = rules.OrderBy(x => x).ToList();
        }

        public TRet Invoke(TArg1 arg1)
        {
            var ruleResult = Evaluate(arg1);
            if(ruleResult.TryGetReturnValue<TRet>(out var value))
            {
                return value;
            }
            if(ruleResult == RuleResult.Continue)
            {
                throw new Exception("No rule produced a result");
            }
            // TODO: Give a better explanation. Perhaps you got the types wrong?
            throw new Exception("Unexpected rule result");
        }

        public IRuleResult Evaluate(TArg1 arg1) => Evaluate(arg1, 0);

        private IRuleResult Evaluate(TArg1 arg1, int startingAt)
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
                    return rule.WrapBody((arg1) => Evaluate(arg1, startingAt + 1), arg1);
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
                    default:
                        // TODO: Give a better explanation. Perhaps you got the types wrong?
                        throw new Exception("Unexpected rule result");
                }
            }
            return RuleResult.Continue;
        }

    }
}
