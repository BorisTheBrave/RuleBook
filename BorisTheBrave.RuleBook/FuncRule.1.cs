using System;

namespace BorisTheBrave.RuleBook
{
    /// <summary>
    /// Represents a single rule that can be inserted into a FuncBook.
    /// Rules can only belong to a single rulebook.
    /// </summary>
    public class FuncRule<TArg1, TRet> : IComparable<FuncRule<TArg1, TRet>>
    {
        private FuncBook<TArg1, TRet>? parent;
        private string? name;
        private float order;
        private int insertionOrder;
        private FuncRule<TArg1, TRet> orderBefore;
        private FuncRule<TArg1, TRet> orderAfter;
        private Func<TArg1, bool>? condition;
        // At most one of the following should be defined
        private Func<TArg1, IRuleResult>? funcBody;
        private Func<Func<TArg1, IRuleResult>, TArg1, IRuleResult>? wrapBody;
        private FuncBook<TArg1, TRet>? bookBody;
        private bool bookBodyFollow;


        public FuncBook<TArg1, TRet>? Parent
        {
            get
            {
                return parent;
            }
            set
            {
                parent?.RemoveRule(this);
                parent = value;
                parent?.AddRule(this);
            }
        }
        /// <summary>
        /// Name is used for looking up rules in a rulebook.
        /// </summary>
        public string? Name { get { return name; } set { name = value; } }
        /// <summary>
        /// The relative order of this rule, lower is earlier.
        /// Updating this will change the position in the rulebook.
        /// </summary>
        public float Order { get { return order; } set { order = value; Reorder(); } }
        internal int InsertionOrder { get { return insertionOrder; } set { insertionOrder = value; } }
        /// <summary>
        /// Sorts this rule immediately before the other rule.
        /// Sets <see cref="FuncRule{TArg1, TRet}.OrderBefore"/>.
        /// Updating this will change the position in the rulebook.
        /// </summary>
        public FuncRule<TArg1, TRet> OrderBefore { get { return orderBefore; } set { orderBefore = value; Reorder(); } }
        /// <summary>
        /// Sorts this rule immediately after the other rule.
        /// Sets <see cref="FuncRule{TArg1, TRet}.OrderBefore"/>.
        /// Updating this will change the position in the rulebook.
        /// </summary>
        public FuncRule<TArg1, TRet> OrderAfter { get { return orderAfter; } set { orderAfter = value; Reorder(); } }

        /// <summary>
        /// If set, the rulebook will evaluate this before the rule body. If it returns false, the rule is skipped.
        /// </summary>
        public Func<TArg1, bool>? Condition { get { return condition; } set { condition = value; Reorder(); } }
        // At most one of the following should be defined
        /// <summary>
        /// The body of a normal rule, called when evaluating it.
        /// Cannot be set with WrapBody or BookBody.
        /// </summary>
        public Func<TArg1, IRuleResult>? FuncBody
        {
            get { return funcBody; }
            set
            {
                if (value != null && (wrapBody != null || bookBody != null)) RaiseBodyError();
                funcBody = value;
            }
        }

        /// <summary>
        /// The body of the wrap rule, called when evaluating it.
        /// Cannot be set with FuncBody or BookBody.
        /// </summary>
        public Func<Func<TArg1, IRuleResult>, TArg1, IRuleResult>? WrapBody
        {
            get { return wrapBody; }
            set
            {
                if (value != null && (funcBody != null || bookBody != null)) RaiseBodyError();
                wrapBody = value;
            }
        }

        /// <summary>
        /// The body of a rulebook rule. This rulebook is evaluated when the rule is.
        /// Cannot be set with FuncBody or WrapBody.
        /// </summary>
        public FuncBook<TArg1, TRet>? BookBody
        {
            get { return bookBody; }
            set
            {
                if (value != null && (funcBody != null || wrapBody != null)) RaiseBodyError();
                bookBody = value;
            }

        }

        /// <summary>
        /// If true, then the result of evaluating BookBody will be discarded, and the rule
        /// will always result in Continue.
        /// </summary>
        public bool BookBodyFollow { get { return bookBodyFollow; } set { bookBodyFollow = value; } }

        // Like BookBody, but with auto-promotion
        /// <summary>
        /// For normal rules, this autopromotes the rule into a rulebook rule containing a single subrule.
        /// For rulebook rules, this returns the rulbook.
        /// 
        /// You can use this to override the behaviour of any specific rule in a rulebook.
        /// </summary>
        public FuncBook<TArg1, TRet> RuleBook
        {
            get
            {
                if (BookBody != null)
                    return BookBody;
                if (WrapBody != null)
                    throw new Exception("Wrap rules cannot be autopromoted into rulebooks");

                // Auto promote
                var subRulebook = new FuncBook<TArg1, TRet>();
                subRulebook.AddRule(new FuncRule<TArg1, TRet>()
                {
                    Name = "original",
                    FuncBody = funcBody,
                });
                FuncBody = null;
                BookBody = subRulebook;
                BookBodyFollow = false;
                return subRulebook;
            }
        }

        public int CompareTo(FuncRule<TArg1, TRet>? other)
        {
            if (other == null) return 1;
            // If orderBefore/After is set, do the comparison as if you had the same
            // position as the referenced rule.
            // Except you are put before/after that rule itself
            // If two rules are both before the same rule, then they tie break with their normal sort rules.
            if (orderBefore != other.orderBefore)
            {
                if (orderBefore != null)
                {
                    if (other == orderBefore) return -1;
                    return orderBefore.CompareTo(other);
                }
                if (other.orderBefore != null)
                {
                    if (this == other.orderBefore) return 1;
                    return this.CompareTo(other.orderBefore);
                }
            }
            if (orderAfter != other.orderAfter)
            {
                if (orderAfter != null)
                {
                    if (other == orderAfter) return 1;
                    return orderAfter.CompareTo(other);
                }
                if (other.orderAfter != null)
                {
                    if (this == other.orderAfter) return -1;
                    return this.CompareTo(other.orderAfter);
                }
            }

            // Normal sort rules
            if (this.Order < other.Order) return -1;
            if (this.Order > other.Order) return 1;
            if (this.Condition != null && other.Condition == null) return -1;
            if (this.Condition == null && other.Condition != null) return 1;
            if (this.InsertionOrder < other.InsertionOrder) return -1;
            if (this.InsertionOrder > other.InsertionOrder) return 1;
            return 0;
        }

        private void Reorder()
        {
            parent?.ReorderRule(this);
        }

        private void RaiseBodyError()
        {
            throw new Exception($"A rule can only have one of FuncBody, WrapBody or BookBody set. Unset the others first");
        }

        public override string ToString()
        {
            return Name ?? base.ToString();
        }
    }
}
