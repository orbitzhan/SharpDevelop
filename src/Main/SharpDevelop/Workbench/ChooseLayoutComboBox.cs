﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Gui;

namespace ICSharpCode.SharpDevelop.Workbench
{
	/// <summary>
	/// Command for layout combobox in toolbar.
	/// </summary>
	class ChooseLayoutComboBox : System.Windows.Controls.ComboBox
	{
		int editIndex  = -1;
		int resetIndex = -1;
		
		public ChooseLayoutComboBox()
		{
			LayoutConfiguration.LayoutChanged += new EventHandler(LayoutChanged);
			SD.ResourceService.LanguageChanged += new EventHandler(ResourceService_LanguageChanged);
			RecreateItems();
		}

		void ResourceService_LanguageChanged(object sender, EventArgs e)
		{
			RecreateItems();
		}
		
		void RecreateItems()
		{
			editingLayout = true;
			try {
				var comboBox = this;
				comboBox.Items.Clear();
				int index = 0;
				foreach (LayoutConfiguration config in LayoutConfiguration.Layouts) {
					if (LayoutConfiguration.CurrentLayoutName == config.Name) {
						index = comboBox.Items.Count;
					}
					comboBox.Items.Add(config);
				}
				editIndex = comboBox.Items.Count;
				
				comboBox.Items.Add(StringParser.Parse("${res:ICSharpCode.SharpDevelop.Commands.ChooseLayoutCommand.EditItem}"));
				
				resetIndex = comboBox.Items.Count;
				comboBox.Items.Add(StringParser.Parse("${res:ICSharpCode.SharpDevelop.Commands.ChooseLayoutCommand.ResetToDefaultItem}"));
				comboBox.SelectedIndex = index;
			} finally {
				editingLayout = false;
			}
		}
		
		int oldItem = 0;
		bool editingLayout;
		
		protected override void OnSelectionChanged(System.Windows.Controls.SelectionChangedEventArgs e)
		{
			base.OnSelectionChanged(e);
			
			if (editingLayout) return;
			LoggingService.Debug("ChooseLayoutCommand.Run()");
			
			var comboBox = this;
			string dataPath   = LayoutConfiguration.DataLayoutPath;
			string configPath = LayoutConfiguration.ConfigLayoutPath;
			if (!Directory.Exists(configPath)) {
				Directory.CreateDirectory(configPath);
			}
			
			if (oldItem != editIndex && oldItem != resetIndex) {
				((WpfWorkbench)SD.Workbench).WorkbenchLayout.StoreConfiguration();
			}
			
			if (comboBox.SelectedIndex == editIndex) {
				editingLayout = true;
				ShowLayoutEditor();
				RecreateItems();
				editingLayout = false;
			} else if (comboBox.SelectedIndex == resetIndex) {
				ResetToDefaults();
			} else {
				LayoutConfiguration config = (LayoutConfiguration)LayoutConfiguration.Layouts[comboBox.SelectedIndex];
				LayoutConfiguration.CurrentLayoutName = config.Name;
			}
			
			oldItem = comboBox.SelectedIndex;
		}
		
		static IEnumerable<string> CustomLayoutNames {
			get {
				foreach (LayoutConfiguration layout in LayoutConfiguration.Layouts) {
					if (layout.Custom) {
						yield return layout.Name;
					}
				}
			}
		}
		
		void ShowLayoutEditor()
		{
			using (Form frm = new Form()) {
				frm.Text = StringParser.Parse("${res:ICSharpCode.SharpDevelop.Commands.ChooseLayoutCommand.EditLayouts.Title}");
				
				StringListEditor ed = new StringListEditor();
				ed.Dock = DockStyle.Fill;
				ed.ManualOrder = false;
				ed.BrowseForDirectory = false;
				ed.TitleText = StringParser.Parse("${res:ICSharpCode.SharpDevelop.Commands.ChooseLayoutCommand.EditLayouts.Label}");
				ed.AddButtonText = StringParser.Parse("${res:ICSharpCode.SharpDevelop.Commands.ChooseLayoutCommand.EditLayouts.AddLayout}");
				
				ed.LoadList(CustomLayoutNames);
				FlowLayoutPanel p = new FlowLayoutPanel();
				p.Dock = DockStyle.Bottom;
				p.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
				
				Button btn = new Button();
				p.Height = btn.Height + 8;
				btn.DialogResult = DialogResult.Cancel;
				btn.Text = ResourceService.GetString("Global.CancelButtonText");
				frm.CancelButton = btn;
				p.Controls.Add(btn);
				
				btn = new Button();
				btn.DialogResult = DialogResult.OK;
				btn.Text = ResourceService.GetString("Global.OKButtonText");
				frm.AcceptButton = btn;
				p.Controls.Add(btn);
				
				frm.Controls.Add(ed);
				frm.Controls.Add(p);
				
				frm.FormBorderStyle = FormBorderStyle.FixedDialog;
				frm.MaximizeBox = false;
				frm.MinimizeBox = false;
				frm.ClientSize = new System.Drawing.Size(400, 300);
				frm.StartPosition = FormStartPosition.CenterParent;
				frm.ShowInTaskbar = false;
				
				if (frm.ShowDialog(SD.WinForms.MainWin32Window) == DialogResult.OK) {
					IList<string> oldNames = new List<string>(CustomLayoutNames);
					IList<string> newNames = ed.GetList();
					// add newly added layouts
					foreach (string newLayoutName in newNames) {
						if (!oldNames.Contains(newLayoutName)) {
							oldNames.Add(newLayoutName);
							LayoutConfiguration.CreateCustom(newLayoutName);
						}
					}
					// remove deleted layouts
					LayoutConfiguration.Layouts.RemoveAll(delegate(LayoutConfiguration lc) {
					                                      	return lc.Custom && !newNames.Contains(lc.Name);
					                                      });
					LayoutConfiguration.SaveCustomLayoutConfiguration();
				}
			}
		}
		
		void ResetToDefaults()
		{
			if (MessageService.AskQuestion("${res:ICSharpCode.SharpDevelop.Commands.ChooseLayoutCommand.ResetToDefaultsQuestion}")) {
				
				foreach (LayoutConfiguration config in LayoutConfiguration.Layouts) {
					string configPath = LayoutConfiguration.ConfigLayoutPath;
					string dataPath   = LayoutConfiguration.DataLayoutPath;
					if (File.Exists(Path.Combine(dataPath, config.FileName)) && File.Exists(Path.Combine(configPath, config.FileName))) {
						try {
							File.Delete(Path.Combine(configPath, config.FileName));
						} catch (Exception) {}
					}
				}
				LayoutConfiguration.ReloadDefaultLayout();
			}
		}
		
		void LayoutChanged(object sender, EventArgs e)
		{
			if (editingLayout) return;
			LoggingService.Debug("ChooseLayoutCommand.LayoutChanged(object,EventArgs)");
			var comboBox = this;
			for (int i = 0; i < comboBox.Items.Count; ++i) {
				if (((LayoutConfiguration)comboBox.Items[i]).Name == LayoutConfiguration.CurrentLayoutName) {
					comboBox.SelectedIndex = i;
					break;
				}
			}
		}
	}
}