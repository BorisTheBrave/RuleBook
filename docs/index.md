# RuleBook

A C# library for modular functions, and actions, heavily inspired by [Inform 7](https://ganelson.github.io/inform-website/)'s [Rulebooks](https://ganelson.github.io/inform-website/book/WI_19_1.html).

**What are rulebooks?**

Rulebooks are essentially a fancy form of C#'s `Func<>` and `Action<>` generics.

`Func<>` and `Action<>` can hold a reference to a C# method, or an lambda expression/statement. But the thing they hold is essentially a black box - you cannot do much with it except run it, or check equality.

RuleBook provies `FuncBook<>` and `ActionBook<>`, which work similarly to their counterparts. But these rulebook objects are built out of individual rules, which can be individually inspected and mutated.

Overall, rulebooks give a systematic way of handling a bunch of useful programming patterns, including events, multiple dispatch, modding and code weaving.

# Example

All that's a bit abstract, so let's look at a concrete example. Suppose you are coding some logic for a game. You might have a function like:

```csharp
void EatFood(Item t)
{
    if (!t.Edible)
    {
        Say("You can't eat that");
        return;
    }
    if (t.Poisoned)
    {
        SetStatus("poisoned");
    }

    Say($"You eat the {t}.");
    IncreaseHealth(t.HealthRecovery);
}

EatFood(thePizza);
```

With rulebooks, you can instead do

```csharp
var EatFood = new ActionBook<Item>();

EatFood.AddRule().When(t => !t.Edible).At(-100).Instead(t => { Say("You can't eat that"); });
EatFood.AddRule().Named("Poisoned").When(t => t.Poisoned).Do(t => { SetStatus("poisoned"); });
EatFood.AddRule().Do(t => {
    Say($"You eat the {t}.");
    IncreaseHealth(t.HealthRecovery);
});

EatFood.Invoke(thePizza);
```

In this toy example, we haven't done much more than made the code less readable than the original!

But there are advantages to this approach. We've registered 3 separate rules for this rulebook. Those rules could be registered anywhere in the code base in any order.

That let's us organize the code better. You can place code for handling eating a magic cake with all the other code for magic cakes, you don't have to put every special case inside the EatFood method.

And we can later start modifying those rules themselves as rulebooks, if we want to override reglar behaviour.

```csharp
// Ignore the Poisoned rule when the player is immune.
EatFood["Poisoned"].Rulebook.AddRule().When(t => Player.HasPoisonImmunity).Stop();
```

This let's you organize your code in a very modular way.

You can find more examples in the Test/Examples folder.

# Usage

First, you must figure out the *type signature* of the rulebook you are creating. How many arguments does it have, and does it return a value?

If the rulebook returns a value, use `FuncBook<>`, otherwise `ActionBook<>`. In either case, you must list the arguments in order as generic type parameters, followed by the return type.

For example, to make a rule book that accepts a `int` and a `double`, and returns a `string`, you'd use `FuncBook<int, double, string>`. 

As a special case, an action book that has no arguments uses the non-generic class `ActionBook`.

Once you have your rulebook object, you can populate it with rules. This is easiest done with the fluent API:

```csharp
var myRulebook = FuncBook<int, double, string>();

myRulebook.AddRule()
    .Named("myRule")
    .At(100)
    .When((i, d) => i > d)
    .Instead((i, d) => {...});
```

Finally, you are ready to run your rulebook.

```
string result = myRulebook.Invoke(1, 1.23);
```

## Defining rules fluently

A builder object is created when you do `myRuleBook.AddRule()`. You can then chain several optional methods after that to configure the rule, then finally set the rule body that shows how to evaluate the rule.

See [Rulebook Semantics](#rulebook-semantics) for the interpretation of these options.

---

Optional fluent methods:

### `.Named`
Gives a name to the rule.
### `.At(int)` / `.OrderBefore(rule)` / `.OrderAfter(rule)`
Controls [the order of the rule in the rulebook.](#rule-ordering)
### `.When(condition)`
Gives a `Func` defining when this rule is active.

---

All rule definitions must end with defining the body of the rule, which uses one of these fluent methods:

### `.Do(body)`
This rule runs the body, then continues to the next rule in the rulebook. Body take the same args as the rulebook, but not return anything.
### `.Instead(body)`
This rule runs the body, then exits the rulebook. Body should be a lambda with the same signature as the rulebook.
### `.Return(value)`
This rule exits the rulebook with agiven value. For `FuncBook<>` only.
### `.Stop()`
This rule exits the rulebook. For `ActionBook<>` only.
### `.WithBody(body)`
This rule runs the body, and either continues to the next rule, or exits the rulebook. Returns an `IRuleResult` described in [Rulebook Semantics](#rulebook-semantics).
### `.Wrap(body)`
 See [wrap rules](#wrap-rules)
### `.WithWrapBody(body)`
As [wrap rules](#wrap-rules), but like `WithBody`, uses `IRuleResult` for the return value.
### `.AbideBy(rulebook)`
Creates a rule that invokes another rulebook, then stopping or returning if that rulebook did so. See [rulebook rules](#rulebook-rules)
### `.Follow(rulebook)`
Creates a rule that invokes another rulebook, ignoring any result of the rulebook (bar throwing an Exception). See [rulebook rules](#rulebook-rules)

## Defining rules from an object

You can also use a more classic API, creating a `FuncRule<>` or `ActionRule<>` object:

```csharp
myRulebook.AddRule(new FuncRule<int, double, string>(){
    Name = "myRule",
    Order = 100,
    Condition = (i, d) => i > d,
    FuncBody = (i, d) => {
        return RuleResult.Return("the result value");
    },
});
```

This lacks some amenities that the rule builder supplies.

# Rulebook semantics

A rulebook consists of a set of rules. When you `Evaluate` a rulebook, it walks through the set of rules [in order](#rule-ordering), and evaluates each one.

To evaluate a rule:

First, then rule `Condition` is checked, if present. If it returns false, the rule is skipped.

Then, the rule body is called, which returns an `IRuleResult` value. This is an enum that operate much like C#'s `Nullable<>` type. It can have values `Continue`/`Stop`/`Return(value)`/`Async`.
* If the returned result is `Continue`, then the rulebook moves on to the next rule.
* If the returned result is `Stop` or `Return` then the rulebook stops evaluation, skipping all later rules. `Stop` is used for `ActionBook` and `Return` for `FuncBook`.
* If the returned result is `Async`, then the actual result is awaited on synchronously or asynchronous.


The `Invoke` method works identical to `Evaluate`, then tries to interpret the `IRuleResult`. For `FuncBook<>`s that finish with a `Return(value)` result, they will throw an exception.


## Special Rules

Normal rules are just store their body as a `Func<>` or `Action<>`, but there are special rules that behave differently.

### Wrap Rules

Wrap rules work a bit like [ASP .NET Core Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-8.0). The body of wrap rules is passed an
additional argument, called the *continuation*. This is a method to call when continuing the process the rule book.

This allows you complete control over further processing of the rulebook, including:
* Putting code before/after other rules, `including try/catch` or `try/finally`
* Changing the arguments the rest of the rules will recieve
* Changing the return value after the rest of the rules have run.
* Skipping the rest of the rules entirely, by not calling the continuation.

Example:

```csharp
// Make a function for getting the displayed name of an object
var FormatName = new FuncBook<Object, string>();
// Add some default behaviour
FormatName.AddRule().At(0).Instead(o => o.Name);
// Add a wrap rule that adds a formal title
FormatRule.AddRule().At(-1).When(o => o == currentKing).Wrap((contuation, o) => "Lord " + continuation(o));
// Add a rule that sets a specific name. 
// This rule is run before the wrapper, so won't have "Lord " prepended even if currentKing == player.
FormatRule.AddRule().At(-2).When(o => o == player).Return("you")
```

### Rulebook Rules

A rule can itself contain a sub rulebook of rules to execute. Nesting rule books like this gives a lot tighter control over the ordering of rules.

If you have a simple rule, and read from `rule.Rulebook`, the library will automatically promote original rule to contain a sub rulebook, where the sub rulebook contains a single
rule with the body of the original rule. This allows you to take an rule, and add special cases to it.

```csharp
myRule.Rulebook.AddRule().When(...).Instead(...);
```


## Rule ordering

If a rule has `OrderBefore`/`OrderAfter`, then it's inserted directly before/after the referenced rule in the sort order.

Otherwise, rules are sorted according to their `Order` property in ascending order. (i.e. order -1 rules run before order 1 rules)

In case of a tie, rules with a condition are ordered before those without.

If still tied, insertion order is preserved.

# Async support

Rulebooks have built in support for Tasks. Rule can be declared async by simply having their body return `Task<>` or `Task`. And rulebooks can be invoked asynchronously with `InvokeAsync`.

Async and sync rules can be freely mixed in one rulebook. Async rules will automatically block if called in a synchronous way.