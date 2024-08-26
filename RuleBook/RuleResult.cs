using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleBook
{

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
            if (typeof(T).IsAssignableTo(typeof(TValue)))
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
}
