using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3NextConfigEditor.Extensions
{
	public static class PropGridExtensions
	{
		public static void SetLabelColumnWidth(this PropertyGrid grid, int width)
		{
			FieldInfo fi = grid?.GetType().GetField("gridView", BindingFlags.Instance | BindingFlags.NonPublic);
			Control view = fi?.GetValue(grid) as Control;
			MethodInfo mi = view?.GetType().GetMethod("MoveSplitterTo", BindingFlags.Instance | BindingFlags.NonPublic);
			mi?.Invoke(view, new object[] { width });
		}
	}
}
