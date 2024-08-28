# Comparison to Inform7

Inform 7's rulebook system was the main inspiration for this library. Unfortunately it's not always been possible to have the same tight integration that Inform 7 offers. Here I discuss features and their equivalents.

## Rulebook Basis

The [basis](https://ganelson.github.io/inform-website/book/WI_19_9.html) of a rulebook is just the arguments. You can specify them in the type arguments of `FuncBook<>` and `ActionBook<>`

Inform 7 supports only a single argument - this library supports up to 8. However, it often makes sense to only have a single argument, which is a custom class with all the details in it. Then adding additional details won't require refactoring existing rules.

## Rulebook Variables

There's no easy equivalent of [rulebook variables](https://ganelson.github.io/inform-website/book/WI_19_10.html). If you need something like this, consider adding an extra argument, which is some sort of mutable object you can fit ths data in.

## Success and Failure

Rulebook [success/failure](https://ganelson.github.io/inform-website/book/WI_19_11.html) is easiest modelled as a `FuncBook<>` with return type `bool`. You can also use an `ActionBook<>` and throw in the case of failure.

Note that "make no decision" in Inform is equivalent to "continue the action". This library simply always says "continue".

There's no exact equivalent to "stop the action". Even [in Inform](https://inform-7-handbook.readthedocs.io/en/latest/chapter_4_actions/rulebooks_&_stop_the_action/), you are recommended to use "rule succeeds" or "rule fails".

## Named outcome and Producing Values

Like success/failure, these features boil down to having a rulebook with an appropriate return type.

## Abide By and Follow

You can use the rule builder methods `.AbideBy` and `.Follow` to make a rule that abdies by or follows another rulebook as its entire body.

If you want to abide by or follow a rulebook in a block of C# code, use the following:

```csharp
// Follow
otherRulebook.Invoke(arg1, arg2);
// Abide by
var ruleResult = otherRulebook.Invoke(arg1, arg2);
if (ruleResult != RuleResult.Continue)
    return ruleResult;
```

There's no equivalent for "abide by anonymously" as the origin of a rule result is not tracked in the first place.

## Sorting rulebooks

This library has much simpler sorting rules than Inform 7. Expect to manually set the order a lot more frequently as a consequence.


## Rulebook default outcomes

Inform rulebooks have a default outcome of one of `continue the action`/`rule succeeds`/`rule fails`, which is applied if the body doesn't mention any. 
For static typing reasons there is no equivalent, and rules must explicitly pick one, usually via `.Do` or `.Instead`. 


## Before, After, Carry Out, etc

This library supplies you with the core implementation of a rulebook. It does not create a bunch of default rulebooks for you to work with. Inform does this as part of the *Standard Rules*.

In Inform 7, Before rules are implemented by inserting rules into a `before` rulebook. Another rulebook, `action-processing rules` is responsible for calling this.

You could approximate Inform's behaviour by creating a similar setup:

```csharp
var actionProcessing = new FuncBook<GameAction, bool>();
var before = new FuncBook<GameAction, bool>();
var instead = new FuncBook<GameAction, bool>();
var carryOut = new FuncBook<GameAction, bool>();
var after = new FuncBook<GameAction, bool>();

actionProcessing.AddRule().Named("before stage rule").AbideBy(before);
actionProcessing.AddRule().Named("instead stage rule").AbideBy(instead);
actionProcessing.AddRule().Named("carry out stage rule").Follow(carryOut);
actionProcessing.AddRule().Named("after stage rule").AbideBy(after);
```

This is not an exact recreation - consult the *Standard Rules* for a better idea of the semantics.

## \[the noun\] etc

Again, this is part of the standard library. Inform simply sets some globals when you start an action, you can do the same.