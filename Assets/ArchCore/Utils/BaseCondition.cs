using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArchCore.Utils.Conditions
{
	public abstract class BaseCondition<T>
	{
		
		public static Condition<T> True { get; }
		public static Condition<T> False { get; }

		static BaseCondition()
		{
			True = new Condition<T>(_=> true);
			False = new Condition<T>(_=> false);
		}
		
		public abstract bool Check(T arg);
		
		public static OrCondition<T> operator |(BaseCondition<T> lhs, BaseCondition<T> rhs)
		{
			if (lhs is OrCondition<T> re)
			{
				re.Add(rhs);
				return re;
			}

			if (rhs is OrCondition<T> le)
			{
				le.Add(rhs);
				return le;
			}

			return new OrCondition<T>(lhs, rhs);
		}
        
		public static AndCondition<T> operator &(BaseCondition<T> lhs, BaseCondition<T> rhs)
		{
			if (lhs is AndCondition<T> re)
			{
				re.Add(rhs);
				return re;
			}

			if (rhs is AndCondition<T> le)
			{
				le.Add(rhs);
				return le;
			}

			return new AndCondition<T>(lhs, rhs);
		}
	}

	public abstract class GroupCondition<T> : BaseCondition<T>
	{
		protected readonly List<BaseCondition<T>> conditions;

		protected GroupCondition()
		{
			conditions = new List<BaseCondition<T>>();
		}

		protected GroupCondition(params BaseCondition<T>[] conditions)
		{
			this.conditions = conditions.ToList();
		}

		protected GroupCondition(IEnumerable<BaseCondition<T>> conditions)
		{
			this.conditions = conditions.ToList();
		}

		public void Add(BaseCondition<T> condition)
		{
			conditions.Add(condition);
		}

	}


	public class AndCondition<T> : GroupCondition<T>
	{
		public AndCondition() : base()
		{
			
		}

		public AndCondition(params BaseCondition<T>[] conditions) : base(conditions)
		{
			
		}

		public AndCondition(IEnumerable<BaseCondition<T>> conditions) : base(conditions)
		{
			
		}
		
		public override bool Check(T arg)
		{
			foreach (var condition in conditions)
			{
				if (!condition.Check(arg)) return false;
			}

			return true;
		}
	}

	public class OrCondition<T> : GroupCondition<T>
	{
		
		public OrCondition() : base()
		{
			
		}

		public OrCondition(params BaseCondition<T>[] conditions) : base(conditions)
		{
			
		}

		public OrCondition(IEnumerable<BaseCondition<T>> conditions) : base(conditions)
		{
			
		}
		
		public override bool Check(T arg)
		{
			foreach (var condition in conditions)
			{
				if (condition.Check(arg)) return true;
			}

			return false;
		}
	}

	public class NotCondition<T> : BaseCondition<T>
	{
		private readonly BaseCondition<T> condition;

		public NotCondition(BaseCondition<T> condition)
		{
			this.condition = condition;
		}

		public override bool Check(T arg)
		{
			return !condition.Check(arg);
		}
	}

	public class Condition<T> : BaseCondition<T>
	{
		private readonly Func<T, bool> func;

		public Condition(Func<T, bool> func)
		{
			this.func = func;
		}

		public override bool Check(T arg)
		{
			return func(arg);
		}
	}
}
