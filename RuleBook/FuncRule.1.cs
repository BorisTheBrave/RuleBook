namespace RuleBook
{
    // TODO Variants for different args, void returns, coroutines, accumulating

    /// <summary>
    /// Represents a single rule that can be inserted into a FuncBook.
    /// Rules can only belong to a single rulebook.
    /// </summary>
    public class FuncRule<TArg1, TRet> : IComparable<FuncRule<TArg1, TRet>>
    {
        private FuncBook<TArg1, TRet>? parent;
        private string? name;
        private int order;
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
        public string? Name { get { return name; } set { name = value; } }
        public int Order { get { return order; } set { order = value; Reorder(); } }
        public Func<TArg1, bool>? Condition { get { return condition; } set { condition = value; Reorder(); } }
        // At most one of the following should be defined
        public Func<TArg1, IRuleResult>? FuncBody
        {
            get { return funcBody; }
            set
            {
                if (value != null && (wrapBody != null || bookBody != null)) RaiseBodyError();
                funcBody = value;
            }
        }
        public Func<Func<TArg1, IRuleResult>, TArg1, IRuleResult>? WrapBody
        {
            get { return wrapBody; }
            set
            {
                if (value != null && (funcBody != null || bookBody != null)) RaiseBodyError();
                wrapBody = value;
            }
        }

        public FuncBook<TArg1, TRet>? BookBody
        {
            get { return bookBody; }
            set
            {
                if (value != null && (funcBody != null || wrapBody != null)) RaiseBodyError();
                bookBody = value;
            }

        }

        public bool BookBodyFollow { get { return bookBodyFollow; } set { bookBodyFollow = value; } }

        public int CompareTo(FuncRule<TArg1, TRet>? other)
        {
            if (this.Order < other.Order) return -1;
            if (this.Order > other.Order) return 1;
            if (this.Condition != null && other.Condition == null) return -1;
            if (this.Condition == null && other.Condition != null) return 1;
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
    }
}
