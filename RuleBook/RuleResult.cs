using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RuleBook
{
    /// <summary>
    /// Represents the results of evaluating a single rule in the rulebook.
    /// The different values indicate how the rulebook should continue processing.
    /// * If the returned result is `Continue`, then the rulebook moves on to the next rule.
    /// * If the returned result is `Stop` or `Return` then the rulebook stops evaluation, skipping all later rules. `Stop` is used for `ActionBook` and `Return` for `FuncBook`.
    /// * If the returned result is `Async`, then the actual result is awaited on synchronously or asynchronous.
    /// </summary>
    public interface IRuleResult
    {
    }

    /// <summary>
    /// Represents a rule result indicating the rulebook should move to the next rule.
    /// </summary>
    public class ContinueRuleResult : IRuleResult
    {
        public static ContinueRuleResult Instance = new ContinueRuleResult();

        private ContinueRuleResult() { }
    }

    /// <summary>
    /// Represents a rule result indicating the rulebook should stop processing. Only used for ActionBooks.
    /// </summary>
    public class StopRuleResult : IRuleResult
    {
        public static StopRuleResult Instance = new StopRuleResult();

        private StopRuleResult() { }
    }

    internal interface IReturnRuleResult
    {
        Type Type { get; }
    }

    /// <summary>
    /// Represents a rule result indicating the rulebook should stop processing and return the given value. Only used for FuncBooks with a corresponding return type.
    /// </summary>
    public class ReturnRuleResult<T> : IRuleResult, IReturnRuleResult
    {
        public ReturnRuleResult(T value)
        {
            Value = value;
        }

        public T Value { get; private set; }

        Type IReturnRuleResult.Type => typeof(T);

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

    /// <summary>
    /// Indicates that the actual result hasn't been computed yet, and the rulebook should wait a bit.
    /// This type is rarely relevant to users as the library almost always wraps it.
    /// </summary>
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
        /// <summary>
        /// Rulebook should continue after this rule.
        /// </summary>
        public static ContinueRuleResult Continue => ContinueRuleResult.Instance;
        /// <summary>
        /// Rulebook should stop after this rule. ActionBook only.
        /// </summary>
        public static StopRuleResult Stop => StopRuleResult.Instance;
        /// <summary>
        /// Rulebook should stop after this rule and return a value. FuncBook only.
        /// </summary>
        public static ReturnRuleResult<T> Return<T>(T value) => new ReturnRuleResult<T>(value);

        /// <summary>
        /// Rulebook should wait for the given IRuleResult, then handle it.
        /// </summary>
        public static IRuleResult WrapAsync(Task<IRuleResult> ruleResultTask)
        {
            return new AsyncRuleResult(ruleResultTask);
        }

        /// <summary>
        /// Rulebook should wait for the given task, then stop and return its value. ActionBook only.
        /// </summary>
        public static IRuleResult ReturnWhen<T>(Task<T> task) => WrapAsync(task.ContinueWith(t => (IRuleResult)Return(t.Result)));

        /// <summary>
        /// Rulebook should wait for the given task, then stop. FuncBook only.
        /// </summary>
        public static IRuleResult StopWhen(Task task) => WrapAsync(task.ContinueWith(t => (IRuleResult)Stop));

    }

    public static class RuleResultExtensions
    {
        /// <summary>
        /// Matches a ReturnRuleResult<TValue> and extracts the value.
        /// </summary>
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
        /// Matches a ReturnRuleResult<TValue> and extracts the value.
        /// </summary>
        public static TValue GetReturnValueOrThrow<TValue>(this IRuleResult ruleResult)
        {
            if (ruleResult.TryGetReturnValue<TValue>(out var value))
            {
                return value;
            }
            FuncBook<TValue>.ThrowUnexpectedRuleResult(ruleResult);
            throw new Exception("Impossible");
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
    }
}
