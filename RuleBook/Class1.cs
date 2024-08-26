namespace RuleBook
{
    #region RuleResult
    public interface IRuleResult
    {

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
        private class Rule
        {
            public string Name;
            public int Phase;
            public int Order;
            public Func<TArg, bool> Pre;
            public Func<TArg, IRuleResult> Func;
        }

        public class RuleBuilder
        {
            private string name;
            private int phase;
            private int order;
            private Func<TArg, bool> pre;
            private Func<TArg, IRuleResult> body;

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

            public RuleBuilder Instead(Func<TArg, TRet> body)
            {
                this.body = (arg1) => RuleResult.Return(body(arg1));
                return this;
            }


            public RuleBuilder Do(Action<TArg> body)
            {
                this.body = (arg) => { body(arg); return RuleResult.Continue; };
                return this;
            }

            public RuleBuilder WithBody(Func<TArg, IRuleResult> body)
            {
                this.body = body;
                return this;
            }
        }


        private List<Rule> rules;

        public FuncBook() 
        {
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
                    throw new Exception("Unexpected rule result");

            }
        }

        public IRuleResult Evaluate(TArg arg)
        {
            foreach (var rule in rules)
            {
                if(rule.Pre != null && !rule.Pre(arg))
                {
                    continue;
                }
                var ruleResult = rule.Func(arg);
                switch (ruleResult)
                {
                    case ReturnRuleResult<TRet> r:
                        return r;
                    case null:
                    case ContinueRuleResult _:
                        continue;
                    case ChangeArgsRuleResult<ValueTuple<TArg>> c:
                        arg = c.NewArgs.Item1;
                        continue;
                    default:
                        throw new Exception("Unexpected rule result");

                }
            }
            return RuleResult.Continue;
        }

    }
}
