using Krypton.Toolkit;
using E3Core.Data;
using E3NextConfigEditor.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace E3NextConfigEditor
{
	public partial class AddSpellEditor : KryptonForm
	{
		SortedDictionary<string, SortedDictionary<string, List<SpellData>>> _spellDataOrganized;
		SortedDictionary<string, SortedDictionary<string, List<SpellData>>> _filteredSpellDataOrganized= new SortedDictionary<string, SortedDictionary<string, List<SpellData>>>();
		List<Bitmap> _spellIcons;
		ImageList _spellIconImageList;
		public SpellData SelectedSpell = null;
		public AddSpellEditor(SortedDictionary<string, SortedDictionary<string, List<SpellData>>> spellDataOrganized, List<Bitmap> spellIcons)
		{
			_spellDataOrganized = spellDataOrganized;
			_spellIcons = spellIcons;
			InitializeComponent();
			var imageList =
		
			_spellIconImageList = new ImageList();
			_spellIconImageList.Images.AddRange(_spellIcons.ToArray());

			PopulateData(_spellDataOrganized);
		}
		public void PopulateData(SortedDictionary<string, SortedDictionary<string, List<SpellData>>> spellData)
		{

			spellTreeView.Nodes.Clear();
			spellTreeView.ImageList = _spellIconImageList;
			foreach (var pair in spellData)
			{
				string cat = pair.Key;
				KryptonTreeNode item = new KryptonTreeNode();
				item.Text = cat;
				

				spellTreeView.Nodes.Add(item);

				foreach(var pair2 in pair.Value)
				{
					string subcat = pair2.Key;
					KryptonTreeNode item2 = new KryptonTreeNode();
					item2.Text = subcat;
					item.Nodes.Add(item2);
					foreach (var spell in pair2.Value)
					{
						if (item.ImageIndex <= 0)
						{
							item.ImageIndex = spell.SpellIcon;
							item.SelectedImageIndex = spell.SpellIcon;
						}
						if (item2.ImageIndex <= 0)
						{
							item2.ImageIndex = spell.SpellIcon;
							item2.SelectedImageIndex = spell.SpellIcon;
						}

						KryptonTreeNode item3 = new KryptonTreeNode();
						item3.Text = spell.CastName;
						item3.ImageIndex = spell.SpellIcon;
						item3.SelectedImageIndex = spell.SpellIcon;
						item3.Tag = spell;
						item2.Nodes.Add(item3);
					}
				}
			}
		}
		
		private void spellTreeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if(spellTreeView.SelectedNode != null )
			{
				if(spellTreeView.SelectedNode.Tag != null)
				{
					if(spellTreeView.SelectedNode.Tag is SpellData)
					{
						addSpellPropertyGrid.SelectedObject = new Models.SpellDataProxy((SpellData)spellTreeView.SelectedNode.Tag);
					}
				}
			}
		}

		private void addSpellButton_Click(object sender, EventArgs e)
		{

			if (spellTreeView.SelectedNode != null)
			{
				if (spellTreeView.SelectedNode.Tag != null)
				{
					if (spellTreeView.SelectedNode.Tag is SpellData)
					{
						SelectedSpell = (SpellData)spellTreeView.SelectedNode.Tag;

					}
				}
			}
			if(SelectedSpell != null)
			{
				this.DialogResult = DialogResult.OK;
				Close();

			}
		}

		private void cancelSpellButton_Click(object sender, EventArgs e)
		{
			SelectedSpell = null;
			this.DialogResult= DialogResult.Cancel;
			Close();
		}

		private void AddSpellEditor_Load(object sender, EventArgs e)
		{
			addSpellPropertyGrid.SetLabelColumnWidth(ConfigEditor._propertyGridWidth);
		}

		private void addSpellPropertyGrid_SizeChanged(object sender, EventArgs e)
		{
			addSpellPropertyGrid.SetLabelColumnWidth(ConfigEditor._propertyGridWidth);
		}

		private void FilterSpellSearchTree(string searchTerm)
		{
			_filteredSpellDataOrganized.Clear();

			

			foreach (var pair in _spellDataOrganized)
			{
				foreach (var pair2 in pair.Value)
				{
					foreach (var spell in pair2.Value)
					{
						if (spell.CastName.IndexOf(searchTerm, 0, StringComparison.OrdinalIgnoreCase) > -1 || String.IsNullOrWhiteSpace(searchTerm))
						{
							SortedDictionary<string, List<SpellData>> level1;
							if (!_filteredSpellDataOrganized.TryGetValue(pair.Key, out level1))
							{
								level1 = new SortedDictionary<string, List<SpellData>>();
								_filteredSpellDataOrganized.Add(pair.Key, level1);
							}
							List<SpellData> level2;
							if (!level1.TryGetValue(pair2.Key, out level2))
							{
								level2 = new List<SpellData>();
								level1.Add(pair2.Key, level2);
							}
							level2.Add(spell);
						}
					}
				}
			}
			//added to the filtered set, now populate data
			PopulateData(_filteredSpellDataOrganized);
		}
		private void searchButton_Click(object sender, EventArgs e)
		{
			string searchString = searchTextBox.Text;
			FilterSpellSearchTree(searchString);
		}

		private void searchTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				e.SuppressKeyPress = true;
				string searchString = searchTextBox.Text;
				FilterSpellSearchTree(searchString);

			}
			e.Handled = true;
		}
	}
}
