using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RuleBook
{
    // Either holds Continue, when the rulebook should continue,
    // or Stop/Return, for when the rulebook should stop (returning something if it's a FuncBook).
    // or Async, indicating a task needs awaiting.
    public interface IRuleResult
    {
    }

    public class ContinueRuleResult : IRuleResult
    {
        public static ContinueRuleResult Instance = new ContinueRuleResult();

        private ContinueRuleResult() { }
    }

    public class StopRuleResult : IRuleResult
    {
        public static StopRuleResult Instance = new StopRuleResult();

        private StopRuleResult() { }
    }


    public class ReturnRuleResult<T> : IRuleResult
    {
        public ReturnRuleResult(T value)
        {
            Value = value;
        }

        public T Value { get; private set; }

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

    public class AsyncRuleResult : IRuleResult
    {
        public AsyncRuleResult(Task<IRuleResult> result)
        {
            Result = result;
        }

        public Task<IRuleResult> Result { get; private set; }

        public override bool Equals(object? obj)
        {
            return obj is AsyncRuleResult result &&
                   EqualityComparer<Task<IRuleResult>>.Default.Equals(Result, result.Result);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Result);
        }
    }

    public static class RuleResult
    {
        public static ContinueRuleResult Continue => ContinueRuleResult.Instance;
        public static StopRuleResult Stop => StopRuleResult.Instance;
        public static ReturnRuleResult<T> Return<T>(T value) => new ReturnRuleResult<T>(value);
        public static IRuleResult WrapAsync(Task<IRuleResult> ruleResultTask)
        {
            if (ruleResultTask.IsCompletedSuccessfully)
                return ruleResultTask.Result;
            return new AsyncRuleResult(ruleResultTask);
        }

        public static IRuleResult ReturnWhen<T>(Task<T> task) => WrapAsync(task.ContinueWith(t => (IRuleResult)Return(t.Result)));
        public static IRuleResult StopWhen(Task task) => WrapAsync(task.ContinueWith(t => (IRuleResult)Stop));

    }

    public static class RuleResultExtensions
    {
        public static bool TryGetReturnValue<TValue>(this IRuleResult ruleResult, out TValue value)
        {
            if (ruleResult is ReturnRuleResult<TValue> r)
            {
                value = r.Value;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Synchronously blocks until ruleResult can be unwrapped from AsyncRuleResult.
        /// </summary>
        public static IRuleResult Wait(this IRuleResult ruleResult)
        {
            while (ruleResult is AsyncRuleResult arr)
            {
                ruleResult = arr.Result.Result;
            }
            return ruleResult;
        }

        /// <summary>
        /// Unwraps AsyncRuleResult.
        /// </summary>
        public static async Task<IRuleResult> Await(this IRuleResult ruleResult)
        {
            while (ruleResult is AsyncRuleResult arr)
            {
                ruleResult = await arr.Result;
            }
            return ruleResult;
        }

        public static TValue GetReturnValueOrThrow<TValue>(this IRuleResult ruleResult)
        {
            if (ruleResult.TryGetReturnValue<TValue>(out var value))
            {
                return value;
            }
            // TODO: Give a better explanation. Perhaps you got the types wrong?
            throw new Exception("Unexpected rule result");
        }

        public static async Task<TValue> GetReturnValueOrThrowAsync<TValue>(this IRuleResult ruleResult)
        {
            if (ruleResult.TryGetReturnValue<TValue>(out var value))
            {
                return value;
            }
            // TODO: Give a better explanation. Perhaps you got the types wrong?
            throw new Exception("Unexpected rule result");
        }
    }
}
