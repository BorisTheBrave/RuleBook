namespace RuleBook
{
    #region RuleResult
    public interface IRuleResult<T>
    {

    }

    public class ContinueRuleResult<T> : IRuleResult<T>
    {
        public static ContinueRuleResult<T> Instance = new ContinueRuleResult<T>();

        private ContinueRuleResult() { }

        public static implicit operator ContinueRuleResult<T>(AnyContinueRuleResult _) => new ContinueRuleResult<T>();

    }

    public class AnyContinueRuleResult
    {
        public static AnyContinueRuleResult Instance = new AnyContinueRuleResult();

        private AnyContinueRuleResult() { }
    }


    public class ReturnRuleResult<T> : IRuleResult<T>
    {
        public ReturnRuleResult(T value)
        {
            Value = value;
        }

        public T Value { get; private set; }
    }

    // TOOD: Stop/AnyStop for Return<void>

    public class ChangeArgsRuleResult<TArgs, T> : IRuleResult<T>
    {
        public ChangeArgsRuleResult(TArgs newArgs)
        {
            NewArgs = newArgs;
        }

        public TArgs NewArgs { get; private set; }
    }

    public static class RuleResult
    {
        public static AnyContinueRuleResult Continue => AnyContinueRuleResult.Instance;
        public static ReturnRuleResult<T> Return<T>(T value) => new ReturnRuleResult<T>(value);
        //public static ChangeArgsRuleResult<ValueTuple<T1>> ChangeArgs<T1>(T1 arg1) => new ChangeArgsRuleResult<ValueTuple<T1>>(new ValueTuple<T1>(arg1));
    }
    #region

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

    // TODO Variants for different args, void returns, coroutines

    public class FuncBook<TArg, TRet>
    {
        private class Rule
        {
            public string Name;
            public int Phase;
            public int Order;
            public Func<TArg, bool> Pre;
            public Func<TArg, IRuleResult<TRet>> Func;
        }

        private List<Rule> rules;

        public FuncBook() 
        {
        }

        public TRet Invoke(TArg arg)
        {
            var ruleResult = AbideBy(arg);
            //TODO: Method for this?
            switch(ruleResult)
            {
                case ReturnRuleResult<TRet> r:
                    return r.Value;
                case ContinueRuleResult<TRet> _:
                case ChangeArgsRuleResult<TRet> _:
                    throw new Exception("No rule produced a result");
                default:
                    throw new Exception("Unexpected rule result");

            }
        }

        public IRuleResult<TRet> AbideBy(TArg arg)
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
                    case ContinueRuleResult<TRet> _:
                        continue;
                    case ChangeArgsRuleResult<ValueTuple<TArg>> c:
                        arg = c.NewArgs.Item1;
                    default:
                        throw new Exception("Unexpected rule result");

                }
            }
        }

    }
}
