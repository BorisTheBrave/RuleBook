using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BorisTheBrave.RuleBook
{
    /// <summary>
    /// A collection of rules, that collectively define a callable function with signature Func<TArg1, TRet>
    /// </summary>
    public class FuncBook<TArg1, TRet>
    {
        // Uses the Curiously Recurring Template Pattern, TSelf will be a derived class of the builder.
        public class SimpleRuleBuilder<TSelf, TBuiltRule> where TSelf : SimpleRuleBuilder<TSelf, TBuiltRule>
        {
            protected Func<FuncRule<TArg1, TRet>, TBuiltRule> finish;


            private protected string name;
            private protected float order;
            private protected Func<TArg1, bool> condition;
            private protected string conditionText;
            private Func<TArg1, IRuleResult> body;

            public SimpleRuleBuilder(Func<FuncRule<TArg1, TRet>, TBuiltRule> finish)
            {
                this.finish = finish;
            }


            /// <summary>
            /// If the condition return false, the rule is skipped.
            /// Sets <see cref="FuncRule{TArg1, TRet}.Condition"/>.
            /// </summary>
#if CSHARP6
            public RuleBuilder When(Func<TArg1, bool> condition, [CallerArgumentExpression(nameof(condition))] string conditionText = null)
#else
            public TSelf When(Func<TArg1, bool> condition, string conditionText = null)
#endif
            {
                if (condition == null)
                {
                    this.condition = condition;
                    this.conditionText = conditionText;
                }
                else
                {
                    var c = this.condition;
                    this.condition = (arg1) => c(arg1) && condition(arg1);
                }
                if (this.conditionText == null)
                {
                    this.conditionText = conditionText;
                }
                else if (conditionText != null)
                {
                    this.conditionText = $"{this.conditionText} && {conditionText}";
                }
                return (TSelf)this;
            }

            /// <summary>
            /// Name is used for looking up rules in a rulebook.
            /// Sets <see cref="FuncRule{TArg1, TRet}.Name"/>.
            /// </summary>
            public TSelf Named(string name)
            {
                this.name = name;
                return (TSelf)this;
            }

            /// <summary>
            /// The relative order of this rule, lower is earlier.
            /// Sets <see cref="FuncRule{TArg1, TRet}.Order"/>.
            /// </summary>
            public TSelf At(float order)
            {
                this.order = order;
                return (TSelf)this;
            }


#if IS_ACTION
            /// <summary>
            /// Sets a body that stops the rulebook. Finishes building the rule.
            /// </summary>
            public TBuiltRule Stop()
            {
                this.body = (arg1) => { return RuleResult.Stop; };
                return Finish();
            }

            /// <summary>
            /// Sets a body that runs some code then stops the rulebook. Finishes building the rule.
            /// </summary>
            public TBuiltRule Instead(Action<TArg1> body)
            {
                this.body = (arg1) => { body(arg1); return RuleResult.Stop; };
                return Finish();
            }

            /// <summary>
            /// Sets a body that runs some async code then stops the rulebook. Finishes building the rule.
            /// </summary>
            public TBuiltRule Instead(Func<TArg1, Task> body)
            {
                this.body = (arg1) => RuleResult.StopWhen(body(arg1));
                return Finish();
            }
#else
            /// <summary>
            /// Sets a body that stops the rulebooks, returning value. Finishes building the rule.
            /// </summary>
            public TBuiltRule Return(TRet value)
            {
                this.body = (arg1) => RuleResult.Return(value);
                return Finish();
            }

            /// <summary>
            /// Sets a body that runs some code, then stops the rulebook, returning a value. Finishes building the rule.
            /// </summary>
            public TBuiltRule Instead(Func<TArg1, TRet> body)
            {
                this.body = (arg1) => RuleResult.Return(body(arg1));
                return Finish();
            }

            /// <summary>
            /// Sets a body that runs some async code, then stops the rulebook, returning a value. Finishes building the rule.
            /// </summary>
            public TBuiltRule Instead(Func<TArg1, Task<TRet>> body)
            {
                this.body = (arg1) => RuleResult.ReturnWhen(body(arg1));
                return Finish();
            }
#endif

            /// <summary>
            /// Sets a body that runs some code, then continues the rulebook. Finishes building the rule.
            /// </summary>
            public TBuiltRule Do(Action<TArg1> body)
            {
                this.body = (arg1) => { body(arg1); return RuleResult.Continue; };
                return Finish();
            }

            /// <summary>
            /// Sets a body that runs some async code, then continues the rulebook. Finishes building the rule.
            /// </summary>
            public TBuiltRule Do(Func<TArg1, Task> body)
            {
                this.body = (arg1) => RuleResult.StopWhen(body(arg1));
                return Finish();
            }

            /// <summary>
            /// Sets a body that runs some code that indicates what the rulebook should do next. Finishes building the rule.
            /// </summary>
            public TBuiltRule WithBody(Func<TArg1, IRuleResult> body)
            {
                this.body = body;
                return Finish();
            }

            /// <summary>
            /// Sets a body that runs some async code that indicates what the rulebook should do next. Finishes building the rule.
            /// </summary>
            public TBuiltRule WithBody(Func<TArg1, Task<IRuleResult>> body)
            {
                this.body = (arg1) => RuleResult.WrapAsync(body(arg1));
                return Finish();
            }

            protected virtual FuncRule<TArg1, TRet> Build()
            {
                return new FuncRule<TArg1, TRet>
                {
                    Name = name ?? conditionText,
                    Order = order,
                    Condition = condition,
                    FuncBody = body,
                };
            }

            protected TBuiltRule Finish()
            {
                return finish(Build());
            }

        }

#if !IS_ZERO
        // This is possibly the ugliest generic I've written in my career.
        // FuncBook<VArg1, TRet>.OfTypeRuleBuilder<VArg1>
        // is a builder that accepts lets you be a FuncRule<TTArg, TRet>
        // but ultimately returns a FuncRule<TArg, TRet>.
        public class OfTypeRuleBuilder<VArg1> : FuncBook<VArg1, TRet>.SimpleRuleBuilder<OfTypeRuleBuilder<VArg1>, FuncRule<TArg1, TRet>>
        {
            public OfTypeRuleBuilder(Func<FuncRule<VArg1, TRet>, FuncRule<TArg1, TRet>> finish) : base(finish)
            {
            }
        }
#endif


        /// <summary>
        /// A typesafe class for building a FuncRule<TArg1, TRet>
        /// </summary>
        public class RuleBuilder : SimpleRuleBuilder<RuleBuilder, FuncRule<TArg1, TRet>>
        {
            private FuncRule<TArg1, TRet> orderBefore;
            private FuncRule<TArg1, TRet> orderAfter;
            private Func<Func<TArg1, IRuleResult>, TArg1, IRuleResult> wrapBody;
            private FuncBook<TArg1, TRet>? bookBody;
            private bool bookBodyFollow = false;

            internal RuleBuilder(FuncBook<TArg1, TRet> parent)
                :base(r =>
                {
                    parent.AddRule(r);
                    return r;
                })
            {
            }

            /// <summary>
            /// Sorts this rule immediately before the other rule.
            /// Sets <see cref="FuncRule{TArg1, TRet}.OrderBefore"/>.
            /// </summary>
            public RuleBuilder InsertBefore(FuncRule<TArg1, TRet> other)
            {
                this.orderBefore = other;
                return this;
            }

            /// <summary>
            /// Sorts this rule immediately after the other rule.
            /// Sets <see cref="FuncRule{TArg1, TRet}.OrderAfter"/>.
            /// </summary>
            public RuleBuilder InsertAfter(FuncRule<TArg1, TRet> other)
            {
                this.orderAfter = other;
                return this;
            }


#if IS_ACTION
            /// <summary>
            /// Sets a body that wraps invocation of the remaining rules in the rulebook. Finishes building the rule.
            /// </summary>
            public ActionRule<TArg1> Wrap(Action<Action<TArg1>, TArg1> wrapBody)
            {
                this.wrapBody = (continuation, arg1) =>
                {
                    void Continue(TArg1 arg1)
                    {
                        continuation(arg1);
                    }
                    wrapBody(Continue, arg1);
                    return RuleResult.Stop;
                };
                return Finish();
            }

            /// <summary>
            /// Sets a body that asynchronously wraps invocation of the remaining rules in the rulebook. Finishes building the rule.
            /// </summary>
            public FuncRule<TArg1, TRet> Wrap(Func<Func<TArg1, Task>, TArg1, Task> wrapBody)
            {
                this.wrapBody = (continuation, arg1) =>
                {
                    async Task Continue(TArg1 arg1)
                    {
                        await continuation(arg1).Await();
                    }
                    return RuleResult.StopWhen(wrapBody(Continue, arg1));
                };
                return Finish();
            }
#else
            /// <summary>
            /// Sets a body that wraps invocation of the remaining rules in the rulebook. Finishes building the rule.
            /// </summary>
            public FuncRule<TArg1, TRet> Wrap(Func<Func<TArg1, TRet>, TArg1, TRet> wrapBody)
            {
                this.wrapBody = (continuation, arg1) =>
                {
                    TRet Continue(TArg1 arg1)
                    {
                        return continuation(arg1).Wait().GetReturnValueOrThrow<TRet>();
                    }
                    return RuleResult.Return(wrapBody(Continue, arg1));
                };
                return Finish();
            }

            /// <summary>
            /// Sets a body that asynchronously wraps invocation of the remaining rules in the rulebook. Finishes building the rule.
            /// </summary>
            public FuncRule<TArg1, TRet> Wrap(Func<Func<TArg1, Task<TRet>>, TArg1, Task<TRet>> wrapBody)
            {
                this.wrapBody = (continuation, arg1) =>
                {
                    async Task<TRet> Continue(TArg1 arg1)
                    {
                        return (await continuation(arg1).Await()).GetReturnValueOrThrow<TRet>();
                    }
                    return RuleResult.ReturnWhen(wrapBody(Continue, arg1));
                };
                return Finish();
            }
#endif

            /// <summary>
            /// Sets a body that wraps evaluation of the remaining rules in the rulebook. Finishes building the rule.
            /// </summary>
            public FuncRule<TArg1, TRet> WithWrapBody(Func<Func<TArg1, IRuleResult>, TArg1, IRuleResult> wrapBody)
            {
                this.wrapBody = wrapBody;
                return Finish();
            }

            /// <summary>
            /// Sets a body that asynchronously wraps evaluation of the remaining rules in the rulebook. Finishes building the rule.
            /// </summary>
            public FuncRule<TArg1, TRet> WithWrapBody(Func<Func<TArg1, Task<IRuleResult>>, TArg1, Task<IRuleResult>> wrapBody)
            {
                this.wrapBody = (continuation, arg1) =>
                {
                    async Task<IRuleResult> Continue(TArg1 arg1)
                    {
                        return (await continuation(arg1).Await());
                    }
                    return RuleResult.WrapAsync(wrapBody(Continue, arg1));
                };
                return Finish();
            }

            /// <summary>
            /// Sets a body calls into another rulebook, and uses its result. Finishes building the rule.
            /// </summary>
            public FuncRule<TArg1, TRet> AbideBy(FuncBook<TArg1, TRet> subFuncBook)
            {
                this.bookBody = subFuncBook;
                this.bookBodyFollow = false;
                return Finish();
            }

            /// <summary>
            /// Sets a body calls into another rulebook, and ignores its result. Finishes building the rule.
            /// </summary>
            public FuncRule<TArg1, TRet> Follow(FuncBook<TArg1, TRet> subFuncBook)
            {
                this.bookBody = subFuncBook;
                this.bookBodyFollow = true;
                return Finish();
            }

            // TODO: Should be able to follow/abide by an ActionBook from a FuncBook?


#if !IS_ZERO
            public OfTypeRuleBuilder<VArg1> OfType<VArg1>() where VArg1 : TArg1
            {
                var thenFinish = this.finish;
                return new OfTypeRuleBuilder<VArg1>(r =>
                {
                    var r2 = new FuncRule<TArg1, TRet>
                    {
                        Name = r.Name,
                        Order = r.Order,
                        OrderAfter = orderAfter,
                        OrderBefore = orderBefore,
                    };
                    // OfTypeRuleBuilder has fewer options, so we can safely assume certain
                    // properties of r are null.
                    if (r.Condition == null)
                    {
                        r2.Condition = (arg1) => arg1 is VArg1 varg1;
                    }
                    else
                    {
                        r2.Condition = (arg1) => arg1 is VArg1 varg1 && r.Condition(varg1);
                    }
                    if(condition != null)
                    {
                        var prevCondition = r2.Condition;
                        r2.Condition = (arg1) => condition(arg1) && prevCondition(arg1);
                    }
                    if (r.FuncBody != null)
                        r2.FuncBody = (arg1) => r.FuncBody((VArg1)arg1);
                    return thenFinish(r2);
                })
                    .Named(name)
                    .At(order);
            }
#endif

            protected override FuncRule<TArg1, TRet> Build()
            {
                var r = base.Build();
                r.OrderBefore = orderBefore;
                r.OrderAfter = orderAfter;
                r.WrapBody = wrapBody;
                r.BookBody = bookBody;
                r.BookBodyFollow = bookBodyFollow;
                return r;
            }
        }

        // This is only ever updated by replacement, so
        // that you can edit rules in the middle of running rules without issue.
        private List<FuncRule<TArg1, TRet>> rules = new List<FuncRule<TArg1, TRet>>();

        private int insertionCount = 0;

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
                var r = rules.ToList();
                r.Add(rule);
                rule.InsertionOrder = insertionCount++;
                rules = r;
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
                var r = rules.ToList();
                r.Remove(rule);
                rules = r;
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

        public FuncRule<TArg1, TRet> this[string name] => rules.SingleOrDefault(x => x.Name == name) ?? throw new KeyNotFoundException("Rule with given name not found in rulebook");

        #region Evaluation

#if IS_ACTION
        public void Invoke(TArg1 arg1)
        {
            var ruleResult = Evaluate(arg1);
            if(ruleResult == RuleResult.Continue || ruleResult == null)
            {
                // Unlike a FuncBook, it's fine for a ActionBook to do literally nothing.
                return;
            }
            if(ruleResult == RuleResult.Stop)
            {
                return;
            }
            ThrowUnexpectedRuleResult(ruleResult);
        }
#else
        public TRet Invoke(TArg1 arg1)
        {
            // This will be caught later, but it's a common error case, so let's make it obvious.
            if (rules.Count == 0)
            {
                throw new Exception("No rules in rulebook, it cannot produce a result.");
            }
            var ruleResult = Evaluate(arg1);
            if (ruleResult == RuleResult.Continue || ruleResult == null)
            {
                throw new Exception("No rule produced a result");
            }
            return ruleResult.GetReturnValueOrThrow<TRet>();
        }
#endif

        public IRuleResult Evaluate(TArg1 arg1) => Evaluate(arg1, rules, 0);

        private static IRuleResult Evaluate(TArg1 arg1, List<FuncRule<TArg1, TRet>> rules, int startingAt)
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
                    return rule.WrapBody((arg1) => Evaluate(arg1, rules, i + 1), arg1);
                }
                else if (rule.BookBody != null)
                {
                    if (rule.BookBodyFollow)
                    {
                        rule.BookBody.Evaluate(arg1);
                        ruleResult = RuleResult.Continue;
                    }
                    else
                    {
                        ruleResult = rule.BookBody.Evaluate(arg1);
                    }
                }
                else
                {
                    ruleResult = RuleResult.Continue;
                }
                ruleResult = ruleResult.Wait();
                switch (ruleResult)
                {
#if IS_ACTION
                    case StopRuleResult _:
                        return RuleResult.Stop;
#else
                    case ReturnRuleResult<TRet> r:
                        return r;
#endif
                    case null:
                    case ContinueRuleResult _:
                        continue;
                    default:
                        ThrowUnexpectedRuleResult(ruleResult);
                        break;
                }
            }
            return RuleResult.Continue;
        }

#if IS_ACTION
        public async Task InvokeAsync(TArg1 arg1)
        {
            var ruleResult = await EvaluateAsync(arg1);
            if(ruleResult == RuleResult.Continue || ruleResult == null)
            {
                // Unlike a FuncBook, it's fine for a ActionBook to do literally nothing.
                return;
            }
            if(ruleResult == RuleResult.Stop)
            {
                return;
            }
            ThrowUnexpectedRuleResult(ruleResult);
        }
#else
        public async Task<TRet> InvokeAsync(TArg1 arg1)
        {
            // This will be caught later, but it's a common error case, so let's make it obvious.
            if(rules.Count == 0)
            {
                throw new Exception("No rules in rulebook, it cannot produce a result.");
            }
            var ruleResult = await EvaluateAsync(arg1);
            if (ruleResult == RuleResult.Continue || ruleResult == null)
            {
                throw new Exception("No rule produced a result.");
            }
            return ruleResult.GetReturnValueOrThrow<TRet>();
        }
#endif

        public Task<IRuleResult> EvaluateAsync(TArg1 arg1) => EvaluateAsync(arg1, rules, 0);

        private static async Task<IRuleResult> EvaluateAsync(TArg1 arg1, List<FuncRule<TArg1, TRet>> rules, int startingAt)
        {
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
                    return rule.WrapBody((arg1) => RuleResult.WrapAsync(EvaluateAsync(arg1, rules, i + 1)), arg1);
                }
                else if (rule.BookBody != null)
                {
                    if (rule.BookBodyFollow)
                    {
                        await rule.BookBody.EvaluateAsync(arg1);
                        ruleResult = RuleResult.Continue;
                    }
                    else
                    {
                        ruleResult = await rule.BookBody.EvaluateAsync(arg1);
                    }
                }
                else
                {
                    ruleResult = RuleResult.Continue;
                }
                ruleResult = await ruleResult.Await();
                switch (ruleResult)
                {
#if IS_ACTION
                    case StopRuleResult _:
                        return RuleResult.Stop;
#else
                    case ReturnRuleResult<TRet> r:
                        return r;
#endif
                    case null:
                    case ContinueRuleResult _:
                        continue;
                    default:
                        ThrowUnexpectedRuleResult(ruleResult);
                        break;
                }
            }
            return RuleResult.Continue;
        }

        #endregion

        [DoesNotReturn]
        internal static void ThrowUnexpectedRuleResult(IRuleResult ruleResult)
        {
#if IS_ACTION
            if (ruleResult is IReturnRuleResult rrr)
            {
                throw new Exception($"Encountered ReturnRuleResult<{rrr.Type}> in a ActionBook. You can't return from an ActionBook. You either want RuleResult.Stop, or should be using a FuncBook");
            }
#else
            if (ruleResult == RuleResult.Stop)
            {
                throw new Exception($"Encountered RuleResult.Stop in a FuncBook. You can't stop a FuncBook, you must return a value. You either want RuleResult.Return, or should be using an ActionBook");
            }
#endif
            throw new Exception($"Unexpected rule result: {ruleResult.GetType().Name}");
        }


        public string FormatTree()
        {
            var sb = new StringBuilder();
            FormatTree(sb, "");
            return sb.ToString();
        }

        private void FormatTree(StringBuilder sb, string indent)
        {
            foreach(var rule in rules)
            {
                var desc = "";
                if (rule.FuncBody != null)
                {
                    desc = "Rule";
                }
                else if(rule.WrapBody != null)
                {
                    desc = "Wrap rule";
                }
                else if(rule.BookBody != null)
                {
                    desc = $"{(rule.BookBodyFollow ? "Follow" : "Abide by")} rulebook rule";
                }
                else
                {
                    throw new Exception();
                }
                if (rule.Name != null)
                {
                    // TODO Better String escaping?
                    desc += $" named \"{rule.Name.Replace("\n", "\\n").Replace("\"", "\\\"")}\"";
                }
                if (rule.BookBody != null)
                {
                    desc += " with rules:";
                }
                sb.AppendLine(indent + desc);
                if(rule.BookBody != null)
                {
                    rule.BookBody.FormatTree(sb, indent + "  ");
                }
                    
            }
        }

    }
}
