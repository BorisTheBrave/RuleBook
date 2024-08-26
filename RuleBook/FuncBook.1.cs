using System.Data;

namespace RuleBook
{
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
