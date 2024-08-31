using System.Data;
using System.Diagnostics.CodeAnalysis;

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
            private float order;
            private FuncRule<TArg1, TRet> orderBefore;
            private FuncRule<TArg1, TRet> orderAfter;
            private Func<TArg1, bool> condition;
            private Func<TArg1, IRuleResult> body;
            private Func<Func<TArg1, IRuleResult>, TArg1, IRuleResult> wrapBody;
            private FuncBook<TArg1, TRet>? bookBody;
            private bool bookBodyFollow = false;

            internal RuleBuilder(FuncBook<TArg1, TRet> parent)
            {
                this.parent = parent;
            }

            public RuleBuilder When(Func<TArg1, bool> pre)
            {
                this.condition = pre;
                return this;
            }

            public RuleBuilder Named(string name)
            {
                this.name = name;
                return this;
            }

            public RuleBuilder At(float order)
            {
                this.order = order;
                return this;
            }

            public RuleBuilder InsertBefore(FuncRule<TArg1, TRet> other)
            {
                this.orderBefore = other;
                return this;
            }

            public RuleBuilder InsertAfter(FuncRule<TArg1, TRet> other)
            {
                this.orderAfter = other;
                return this;
            }

#if IS_ACTION
            public ActionRule<TArg1> Stop()
            {
                this.body = (arg1) => { return RuleResult.Stop; };
                return Finish();
            }

            public ActionRule<TArg1> Instead(Action<TArg1> body)
            {
                this.body = (arg1) => { body(arg1); return RuleResult.Stop; };
                return Finish();
            }

            public ActionRule<TArg1> Instead(Func<TArg1, Task> body)
            {
                this.body = (arg1) => RuleResult.StopWhen(body(arg1));
                return Finish();
            }
#else
            public FuncRule<TArg1, TRet> Return(TRet value)
            {
                this.body = (arg1) => RuleResult.Return(value);
                return Finish();
            }

            public FuncRule<TArg1, TRet> Instead(Func<TArg1, TRet> body)
            {
                this.body = (arg1) => RuleResult.Return(body(arg1));
                return Finish();
            }
            public FuncRule<TArg1, TRet> Instead(Func<TArg1, Task<TRet>> body)
            {
                this.body = (arg1) => RuleResult.ReturnWhen(body(arg1));
                return Finish();
            }
#endif

            public FuncRule<TArg1, TRet> Do(Action<TArg1> body)
            {
                this.body = (arg1) => { body(arg1); return RuleResult.Continue; };
                return Finish();
            }

            public FuncRule<TArg1, TRet> Do(Func<TArg1, Task> body)
            {
                this.body = (arg1) => RuleResult.StopWhen(body(arg1));
                return Finish();
            }

            public FuncRule<TArg1, TRet> WithBody(Func<TArg1, IRuleResult> body)
            {
                this.body = body;
                return Finish();
            }

            public FuncRule<TArg1, TRet> WithBody(Func<TArg1, Task<IRuleResult>> body)
            {
                this.body = (arg1) => RuleResult.WrapAsync(body(arg1));
                return Finish();
            }

#if IS_ACTION
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
#else
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

            public FuncRule<TArg1, TRet> WithWrapBody(Func<Func<TArg1, IRuleResult>, TArg1, IRuleResult> wrapBody)
            {
                this.wrapBody = wrapBody;
                return Finish();
            }

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

            public FuncRule<TArg1, TRet> AbideBy(FuncBook<TArg1, TRet> subFuncBook)
            {
                this.bookBody = subFuncBook;
                this.bookBodyFollow = false;
                return Finish();
            }

            public FuncRule<TArg1, TRet> Follow(FuncBook<TArg1, TRet> subFuncBook)
            {
                this.bookBody = subFuncBook;
                this.bookBodyFollow = true;
                return Finish();
            }


            private FuncRule<TArg1, TRet> Finish()
            {
                var rule = new FuncRule<TArg1, TRet>
                {
                    Name = name,
                    Order = order,
                    OrderBefore = orderBefore,
                    OrderAfter = orderAfter,
                    Condition = condition,
                    FuncBody = body,
                    WrapBody = wrapBody,
                    BookBody = bookBody,
                    BookBodyFollow = bookBodyFollow,
                };
                parent.AddRule(rule);
                return rule;
            }
        }


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
                rules.Add(rule);
                rule.InsertionOrder = insertionCount++;
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

        public FuncRule<TArg1, TRet> this[string name] => rules.SingleOrDefault(x => x.Name == name) ?? throw new KeyNotFoundException("Rule with given name not found in rulebook");

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

        public Task<IRuleResult> EvaluateAsync(TArg1 arg1) => EvaluateAsync(arg1, 0);

        private async Task<IRuleResult> EvaluateAsync(TArg1 arg1, int startingAt)
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
                    return rule.WrapBody((arg1) => RuleResult.WrapAsync(EvaluateAsync(arg1, startingAt + 1)), arg1);
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

    }
}
