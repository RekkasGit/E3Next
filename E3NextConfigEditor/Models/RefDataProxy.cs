using ComponentFactory.Krypton.Toolkit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3NextConfigEditor.Models
{
	public class Ref<T>
	{
		private Func<T> getter;
		private Action<T> setter;
		KryptonListItem _listItem;
		KryptonListBox _listBox;
		public Ref(Func<T> getter, Action<T> setter)
		{
			this.getter = getter;
			this.setter = setter;
		}
		[Category("Value Data")]
		[Description("Value")]
		public T Value
		{
			get { 
				return getter(); 
			}
			set { 
				if(_listItem != null && _listBox!=null)
				{
					_listItem.ShortText = value.ToString();
					_listBox.Refresh();
					
				}
				setter(value); 
			
			}
		}
		public KryptonListBox ListBox
		{
			
			set
			{

				_listBox = value;

			}
		}
		public KryptonListItem ListItem
		{
			
			set
			{

				_listItem = value;

			}
		}
	}
}
