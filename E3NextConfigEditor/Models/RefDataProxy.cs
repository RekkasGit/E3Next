using Krypton.Toolkit;
using System;
using System.ComponentModel;

namespace E3NextConfigEditor.Models
{
	public class Ref<T>
	{
		private Func<T> getter;
		private Action<T> setter;
		KryptonListItem _listItem;
		KryptonListBox _listBox;
		bool _refreshList = false;
		public Ref(Func<T> getter, Action<T> setter, bool refreshList = false)
		{
			this.getter = getter;
			this.setter = setter;
			_refreshList = refreshList;
		}
		[Category("Value Data")]
		[Description("Value")]
		public T Value
		{
			get { 
				return getter(); 
			}
			set { 
				if( _refreshList && _listItem != null && _listBox!=null)
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
	public class Ref<T,K>
	{
		private Func<T> getter;
		private Action<T> setter;
		private Func<K> keyGetter;
		KryptonListItem _listItem;
		KryptonListBox _listBox;
		bool _refreshList = false;
		public Ref(Func<T> getter, Action<T> setter,Func<K> keyGetter, bool refreshList = false)
		{
			this.getter = getter;
			this.setter = setter;
			this.keyGetter = keyGetter;
			_refreshList = refreshList;
		}
		[Category("Value Data")]
		[Description("Value")]
		public T Value
		{
			get
			{
				return getter();
			}
			set
			{
				if (_refreshList && _listItem != null && _listBox != null)
				{
					_listItem.ShortText = value.ToString();
					_listBox.Refresh();

				}
				setter(value);

			}
		}
		[Category("Key Data")]
		[Description("Value")]
		public K Key
		{
			get
			{
				return keyGetter();
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
