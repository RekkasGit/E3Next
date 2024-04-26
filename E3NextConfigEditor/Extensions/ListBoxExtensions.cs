using ComponentFactory.Krypton.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3NextConfigEditor.Extensions
{
	public static class KryptonListBoxExtension
	{
		public static void MoveSelectedItemUp(this KryptonListBox listBox)
		{
			_MoveSelectedItem(listBox, -1);
		}

		public static void MoveSelectedItemDown(this KryptonListBox listBox)
		{
			_MoveSelectedItem(listBox, 1);
		}

		static void _MoveSelectedItem(KryptonListBox listBox, int direction)
		{
			// Checking selected item
			if (listBox.SelectedItem == null || listBox.SelectedIndex < 0)
				return; // No selected item - nothing to do
			// Calculate new index using move direction
			int newIndex = listBox.SelectedIndex + direction;
			// Checking bounds of the range
			if (newIndex < 0 || newIndex >= listBox.Items.Count)
				return; // Index out of range - nothing to do

			object selected = listBox.SelectedItem;

			// Removing removable element
			listBox.Items.Remove(selected);
			// Insert it in new position
			listBox.Items.Insert(newIndex, selected);
			// Restore selection
			listBox.SetSelected(newIndex, true);
		}
	}
}
