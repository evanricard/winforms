﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Design;
using System.Reflection;
using System.Windows.Forms.VisualStyles;
using System.Globalization;

namespace System.Windows.Forms.Design.Editors.System.ComponentModel.Design
{
    /// <devdoc>
    ///    Provides a generic editor for most any collection.
    /// </devdoc>
    public class CollectionEditor : UITypeEditor
    { 
        private readonly Type _type;
        private Type _collectionItemType;
        private Type[] _newItemTypes;
        private ITypeDescriptorContext _currentContext;

        private bool _ignoreChangedEvents;
        private bool _ignoreChangingEvents;

        /// <devdoc>
        ///       Useful for derived classes to do processing when cancelling changes
        /// </devdoc>
        protected virtual void CancelChanges()
        {
        }

        /// <devdoc>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.CollectionEditor'/> class using the specified collection type.
        /// </devdoc>
        public CollectionEditor(Type type)
        {
            _type = type;
        }

        /// <devdoc>
        ///    Gets or sets the data type of each item in the collection.
        /// </devdoc>
        protected Type CollectionItemType
        {
            get
            {
                if (_collectionItemType == null)
                {
                    _collectionItemType = CreateCollectionItemType();
                }
                return _collectionItemType;
            }
        }

        /// <devdoc>
        ///       Gets or sets the type of the collection.
        /// </devdoc>
        protected Type CollectionType
        {
            get
            {
                return _type;
            }
        }

        /// <devdoc>
        ///       Gets or sets a type descriptor that indicates the current context.
        /// </devdoc>
        protected ITypeDescriptorContext Context
        {
            get
            {
                return _currentContext;
            }
        }

        /// <devdoc>
        ///    Gets or sets the available item types that can be created for this collection.
        /// </devdoc>
        protected Type[] NewItemTypes
        {
            get
            {
                if (_newItemTypes == null)
                {
                    _newItemTypes = CreateNewItemTypes();
                }
                return _newItemTypes;
            }
        }

        /// <devdoc>
        ///    Gets the help topic to display for the dialog help button or pressing F1. Override to display a different help topic.
        /// </devdoc>
        protected virtual string HelpTopic
        {
            get
            {
                return "net.ComponentModel.CollectionEditor";
            }
        }

        /// <devdoc>
        ///    Gets or sets a value indicating whether original members of the collection can be removed.
        /// </devdoc>
        protected virtual bool CanRemoveInstance(object value)
        {
            if (value is IComponent comp)
            {
                // Make sure the component is not being inherited -- we can't delete these!
                //
                InheritanceAttribute ia = (InheritanceAttribute)TypeDescriptor.GetAttributes(comp)[typeof(InheritanceAttribute)];
                if (ia != null && ia.InheritanceLevel != InheritanceLevel.NotInherited)
                {
                    return false;
                }
            }

            return true;
        }

        /// <devdoc>
        ///    Gets or sets a value indicating whether multiple collection members can be selected.
        /// </devdoc>
        protected virtual bool CanSelectMultipleInstances()
        {
            return true;
        }

        /// <devdoc>
        ///    Creates a new form to show the current collection.
        /// </devdoc>
        protected virtual CollectionForm CreateCollectionForm()
        {
            return new CollectionEditorCollectionForm(this);
        }

        /// <devdoc>
        ///       Creates a new instance of the specified collection item type.
        /// </devdoc>
        protected virtual object CreateInstance(Type itemType)
        {
            return CollectionEditor.CreateInstance(itemType, (IDesignerHost)GetService(typeof(IDesignerHost)), null);
        }

        /// <devdoc>
        ///       This Function gets the object from the givem object. The input is an arrayList returned as an Object.
        ///       The output is a arraylist which contains the individual objects that need to be created.
        /// </devdoc>
        protected virtual IList GetObjectsFromInstance(object instance)
        {
            ArrayList ret = new ArrayList
            {
                instance
            };
            return ret;
        }

        internal static object CreateInstance(Type itemType, IDesignerHost host, string name)
        {
            object instance = null;

            if (typeof(IComponent).IsAssignableFrom(itemType))
            {
                if (host != null)
                {
                    instance = host.CreateComponent(itemType, (string)name);

                    // Set component defaults
                    if (host != null)
                    {
                        if (host.GetDesigner((IComponent)instance) is IComponentInitializer init)
                        {
                            init.InitializeNewComponent(null);
                        }
                    }
                }
            }

            if (instance == null)
            {
                instance = TypeDescriptor.CreateInstance(host, itemType, null, null);
            }

            return instance;
        }


        /// <devdoc>
        ///      Retrieves the display text for the given list item.
        /// </devdoc>
        protected virtual string GetDisplayText(object value)
        {
            string text;

            if (value == null)
            {
                return string.Empty;
            }

            PropertyDescriptor prop = TypeDescriptor.GetProperties(value)["Name"];
            if (prop != null && prop.PropertyType == typeof(string))
            {
                text = (string)prop.GetValue(value);
                if (text != null && text.Length > 0)
                {
                    return text;
                }
            }

            prop = TypeDescriptor.GetDefaultProperty(CollectionType);
            if (prop != null && prop.PropertyType == typeof(string))
            {
                text = (string)prop.GetValue(value);
                if (text != null && text.Length > 0)
                {
                    return text;
                }
            }

            text = TypeDescriptor.GetConverter(value).ConvertToString(value);

            if (text == null || text.Length == 0)
            {
                text = value.GetType().Name;
            }

            return text;
        }

        /// <devdoc>
        ///       Gets an instance of the data type this collection contains.
        /// </devdoc>
        protected virtual Type CreateCollectionItemType()
        {
            PropertyInfo[] props = TypeDescriptor.GetReflectionType(CollectionType).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            for (int i = 0; i < props.Length; i++)
            {
                if (props[i].Name.Equals("Item") || props[i].Name.Equals("Items"))
                {
                    return props[i].PropertyType;
                }
            }

            // Couldn't find anything.  Return Object

            Debug.Fail("Collection " + CollectionType.FullName + " contains no Item or Items property so we cannot display and edit any values");
            return typeof(object);
        }

        /// <devdoc>
        ///       Gets the data types this collection editor can create.
        /// </devdoc>
        protected virtual Type[] CreateNewItemTypes()
        {
            return new Type[] { CollectionItemType };
        }

        /// <devdoc>
        ///       Destroys the specified instance of the object.
        /// </devdoc>
        protected virtual void DestroyInstance(object instance)
        {
            if (instance is IComponent compInstance)
            {
                IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                if (host != null)
                {
                    host.DestroyComponent(compInstance);
                }
                else
                {
                    compInstance.Dispose();
                }
            }
            else
            {
                if (instance is IDisposable dispInstance)
                {
                    dispInstance.Dispose();
                }
            }
        }

        /// <devdoc>
        ///    Edits the specified object value using the editor style  provided by <see cref='System.ComponentModel.Design.CollectionEditor.GetEditStyle'/>.
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (provider != null)
            {
                IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

                if (edSvc != null)
                {
                    _currentContext = context;

                    // Always create a new CollectionForm.  We used to do reuse the form in V1 and Everett
                    // but this implies that the form will never be disposed.
                    // child modal dialog -launching in System Aware mode 
                    //CollectionForm localCollectionForm = DpiHelper.CreateInstanceInSystemAwareContext(() => CreateCollectionForm());
                    ITypeDescriptorContext lastContext = _currentContext;
                    //localCollectionForm.EditValue = value;
                    _ignoreChangingEvents = false;
                    _ignoreChangedEvents = false;
                    DesignerTransaction trans = null;

                    bool commitChange = true;
                    IComponentChangeService cs = null;
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));

                    try
                    {
                        try
                        {
                            if (host != null)
                            {
                                trans = host.CreateTransaction(SR.GetResourceString(SR.CollectionEditorUndoBatchDesc, CollectionItemType.Name));
                            }
                        }
                        catch (CheckoutException cxe)
                        {
                            if (cxe == CheckoutException.Canceled)
                                return value;

                            throw cxe;
                        }

                        cs = host != null ? (IComponentChangeService)host.GetService(typeof(IComponentChangeService)) : null;

                        if (cs != null)
                        {
                            cs.ComponentChanged += new ComponentChangedEventHandler(OnComponentChanged);
                            cs.ComponentChanging += new ComponentChangingEventHandler(OnComponentChanging);
                        }

                        //if (localCollectionForm.ShowEditorDialog(edSvc) == DialogResult.OK)
                        //{
                        //    value = localCollectionForm.EditValue;
                       //}
                        //else
                        //{
                            commitChange = false;
                        //}
                    }
                    finally
                    {
                        //localCollectionForm.EditValue = null;
                        _currentContext = lastContext;
                        if (trans != null)
                        {
                            if (commitChange)
                            {
                                trans.Commit();
                            }
                            else
                            {
                                trans.Cancel();
                            }
                        }

                        if (cs != null)
                        {
                            cs.ComponentChanged -= new ComponentChangedEventHandler(OnComponentChanged);
                            cs.ComponentChanging -= new ComponentChangingEventHandler(OnComponentChanging);
                        }

                        //localCollectionForm.Dispose();
                    }
                }
            }


            return value;
        }

        /// <devdoc>
        ///    Gets the editing style of the Edit method.
        /// </devdoc>
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")] // everything in this assembly is full trust.
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        private bool IsAnyObjectInheritedReadOnly(object[] items)
        {
            // If the object implements IComponent, and is not sited, check with
            // the inheritance service (if it exists) to see if this is a component
            // that is being inherited from another class.  If it is, then we do
            // not want to place it in the collection editor.  If the inheritance service
            // chose not to site the component, that indicates it should be hidden from 
            // the user.

            IInheritanceService iSvc = null;
            bool checkISvc = false;

            foreach (object o in items)
            {
                if (o is IComponent comp && comp.Site == null)
                {
                    if (!checkISvc)
                    {
                        checkISvc = true;
                        if (Context != null)
                        {
                            iSvc = (IInheritanceService)Context.GetService(typeof(IInheritanceService));
                        }
                    }

                    if (iSvc != null && iSvc.GetInheritanceAttribute(comp).Equals(InheritanceAttribute.InheritedReadOnly))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <devdoc>
        ///    Converts the specified collection into an array of objects.
        /// </devdoc>
        protected virtual object[] GetItems(object editValue)
        {
            if (editValue != null)
            {
                // We look to see if the value implements ICollection, and if it does, 
                // we set through that.
                //
                if (editValue is Collections.ICollection)
                {
                    ArrayList list = new ArrayList();

                    Collections.ICollection col = (Collections.ICollection)editValue;
                    foreach (object o in col)
                    {
                        list.Add(o);
                    }

                    object[] values = new object[list.Count];
                    list.CopyTo(values, 0);
                    return values;
                }
            }

            return new object[0];
        }

        /// <devdoc>
        ///       Gets the requested service, if it is available.
        /// </devdoc>
        protected object GetService(Type serviceType)
        {
            if (Context != null)
            {
                return Context.GetService(serviceType);
            }
            return null;
        }

        /// <devdoc>
        /// reflect any change events to the instance object
        /// </devdoc>
        private void OnComponentChanged(object sender, ComponentChangedEventArgs e)
        {
            if (!_ignoreChangedEvents && sender != Context.Instance)
            {
                _ignoreChangedEvents = true;
                Context.OnComponentChanged();
            }
        }

        /// <devdoc>
        ///  reflect any changed events to the instance object
        /// </devdoc>
        private void OnComponentChanging(object sender, ComponentChangingEventArgs e)
        {
            if (!_ignoreChangingEvents && sender != Context.Instance)
            {
                _ignoreChangingEvents = true;
                Context.OnComponentChanging();
            }
        }

        /// <devdoc>
        ///       Removes the item from the column header from the listview column header collection
        /// </devdoc>
        internal virtual void OnItemRemoving(object item)
        {
        }

        /// <devdoc>
        ///       Sets the specified collection to have the specified array of items.
        /// </devdoc>
        protected virtual object SetItems(object editValue, object[] value)
        {
            if (editValue != null)
            {
                // We look to see if the value implements IList, and if it does, 
                // we set through that.
                //
                Debug.Assert(editValue is Collections.IList, "editValue is not an IList");
                if (editValue is Collections.IList list)
                {
                    list.Clear();
                    for (int i = 0; i < value.Length; i++)
                    {
                        list.Add(value[i]);
                    }
                }
            }
            return editValue;
        }

        /// <devdoc>
        ///       Called when the help button is clicked.
        /// </devdoc>
        protected virtual void ShowHelp()
        {
            if (GetService(typeof(IHelpService)) is IHelpService helpService)
            {
                helpService.ShowHelpFromKeyword(HelpTopic);
            }
            else
            {
                Debug.Fail("Unable to get IHelpService.");
            }
        }

        internal class SplitButton : Button
        {
            private PushButtonState _state;
            private const int PushButtonWidth = 14;
            private Rectangle _dropDownRectangle = new Rectangle();
            private bool _showSplit = false;

            private static bool s_isScalingInitialized = false;
            private const int OFFSET_2PIXELS = 2;
            private static int s_offset2X = OFFSET_2PIXELS;
            private static int s_offset2Y = OFFSET_2PIXELS;

            public SplitButton()
                : base()
            {
                if (!s_isScalingInitialized)
                {
                    if (DpiHelper.IsScalingRequired)
                    {
                        s_offset2X = DpiHelper.LogicalToDeviceUnitsX(OFFSET_2PIXELS);
                        s_offset2Y = DpiHelper.LogicalToDeviceUnitsY(OFFSET_2PIXELS);
                    }
                    s_isScalingInitialized = true;
                }
            }

            public bool ShowSplit
            {
                set
                {
                    if (value != _showSplit)
                    {
                        _showSplit = value;
                        Invalidate();
                    }
                }
            }

            private PushButtonState State
            {
                get
                {
                    return _state;
                }
                set
                {
                    if (!_state.Equals(value))
                    {
                        _state = value;
                        Invalidate();
                    }
                }
            }

            public override Size GetPreferredSize(Size proposedSize)
            {
                Size preferredSize = base.GetPreferredSize(proposedSize);
                if (_showSplit && !string.IsNullOrEmpty(Text) && TextRenderer.MeasureText(Text, Font).Width + PushButtonWidth > preferredSize.Width)
                {
                    return preferredSize + new Size(PushButtonWidth, 0);
                }

                return preferredSize;
            }

            protected override bool IsInputKey(Keys keyData)
            {
                if (keyData.Equals(Keys.Down) && _showSplit)
                {
                    return true;
                }
                else
                {
                    return base.IsInputKey(keyData);
                }
            }

            protected override void OnGotFocus(EventArgs e)
            {
                if (!_showSplit)
                {
                    base.OnGotFocus(e);
                    return;
                }

                if (!State.Equals(PushButtonState.Pressed) && !State.Equals(PushButtonState.Disabled))
                {
                    State = PushButtonState.Default;
                }
            }

            protected override void OnKeyDown(KeyEventArgs kevent)
            {
                if (kevent.KeyCode.Equals(Keys.Down) && _showSplit)
                {
                    ShowContextMenuStrip();
                }
                else
                {
                    // Fix for Dev10 bug 462144. We need to pass the unhandled characters 
                    // (including Keys.Space) on to base.OnKeyDown when it's not to
                    // drop the split menu
                    base.OnKeyDown(kevent);
                }
            }

            protected override void OnLostFocus(EventArgs e)
            {
                if (!_showSplit)
                {
                    base.OnLostFocus(e);
                    return;
                }
                if (!State.Equals(PushButtonState.Pressed) && !State.Equals(PushButtonState.Disabled))
                {
                    State = PushButtonState.Normal;
                }
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                if (!_showSplit)
                {
                    base.OnMouseDown(e);
                    return;
                }

                if (_dropDownRectangle.Contains(e.Location))
                {
                    ShowContextMenuStrip();
                }
                else
                {
                    State = PushButtonState.Pressed;
                }
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                if (!_showSplit)
                {
                    base.OnMouseEnter(e);
                    return;
                }

                if (!State.Equals(PushButtonState.Pressed) && !State.Equals(PushButtonState.Disabled))
                {
                    State = PushButtonState.Hot;
                }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                if (!_showSplit)
                {
                    base.OnMouseLeave(e);
                    return;
                }

                if (!State.Equals(PushButtonState.Pressed) && !State.Equals(PushButtonState.Disabled))
                {
                    if (Focused)
                    {
                        State = PushButtonState.Default;
                    }
                    else
                    {
                        State = PushButtonState.Normal;
                    }
                }
            }

            protected override void OnMouseUp(MouseEventArgs mevent)
            {
                if (!_showSplit)
                {
                    base.OnMouseUp(mevent);
                    return;
                }

                if (ContextMenuStrip == null || !ContextMenuStrip.Visible)
                {
                    SetButtonDrawState();
                    if (Bounds.Contains(Parent.PointToClient(Cursor.Position)) && !_dropDownRectangle.Contains(mevent.Location))
                    {
                        OnClick(new EventArgs());
                    }
                }
            }

            protected override void OnPaint(PaintEventArgs pevent)
            {
                base.OnPaint(pevent);

                if (!_showSplit)
                {
                    return;
                }

                Graphics g = pevent.Graphics;
                Rectangle bounds = new Rectangle(0, 0, Width, Height);
                TextFormatFlags formatFlags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;

                ButtonRenderer.DrawButton(g, bounds, State);

                _dropDownRectangle = new Rectangle(bounds.Right - PushButtonWidth - 1, 4, PushButtonWidth, bounds.Height - 8);


                if (RightToLeft == RightToLeft.Yes)
                {
                    _dropDownRectangle.X = bounds.Left + 1;

                    g.DrawLine(SystemPens.ButtonHighlight, bounds.Left + PushButtonWidth, 4, bounds.Left + PushButtonWidth, bounds.Bottom - 4);
                    g.DrawLine(SystemPens.ButtonHighlight, bounds.Left + PushButtonWidth + 1, 4, bounds.Left + PushButtonWidth + 1, bounds.Bottom - 4);
                    bounds.Offset(PushButtonWidth, 0);
                    bounds.Width -= PushButtonWidth;
                }
                else
                {
                    g.DrawLine(SystemPens.ButtonHighlight, bounds.Right - PushButtonWidth, 4, bounds.Right - PushButtonWidth, bounds.Bottom - 4);
                    g.DrawLine(SystemPens.ButtonHighlight, bounds.Right - PushButtonWidth - 1, 4, bounds.Right - PushButtonWidth - 1, bounds.Bottom - 4);
                    bounds.Width -= PushButtonWidth;
                }

                PaintArrow(g, _dropDownRectangle);

                // If we dont' use mnemonic, set formatFlag to NoPrefix as this will show ampersand.
                if (!UseMnemonic)
                {
                    formatFlags |= TextFormatFlags.NoPrefix;
                }
                else if (!ShowKeyboardCues)
                {
                    formatFlags |= TextFormatFlags.HidePrefix;
                }

                if (!string.IsNullOrEmpty(Text))
                {
                    TextRenderer.DrawText(g, Text, Font, bounds, SystemColors.ControlText, formatFlags);
                }

                if (Focused)
                {
                    bounds.Inflate(-4, -4);
                    //ControlPaint.DrawFocusRectangle(g, bounds);
                }
            }

            private void PaintArrow(Graphics g, Rectangle dropDownRect)
            {
                Point middle = new Point(Convert.ToInt32(dropDownRect.Left + dropDownRect.Width / 2), Convert.ToInt32(dropDownRect.Top + dropDownRect.Height / 2));

                //if the width is odd - favor pushing it over one pixel right.
                middle.X += (dropDownRect.Width % 2);

                Point[] arrow = new Point[] {
                    new Point(middle.X - s_offset2X, middle.Y - 1),
                    new Point(middle.X + s_offset2X + 1, middle.Y - 1),
                    new Point(middle.X, middle.Y + s_offset2Y)
                };

                g.FillPolygon(SystemBrushes.ControlText, arrow);
            }

            private void ShowContextMenuStrip()
            {
                State = PushButtonState.Pressed;
                if (ContextMenuStrip != null)
                {
                    ContextMenuStrip.Closed += new ToolStripDropDownClosedEventHandler(ContextMenuStrip_Closed);
                    ContextMenuStrip.Show(this, 0, Height);
                }
            }

            private void ContextMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
            {
                if (sender is ContextMenuStrip cms)
                {
                    cms.Closed -= new ToolStripDropDownClosedEventHandler(ContextMenuStrip_Closed);
                }

                SetButtonDrawState();
            }

            private void SetButtonDrawState()
            {
                if (Bounds.Contains(Parent.PointToClient(Cursor.Position)))
                {
                    State = PushButtonState.Hot;
                }
                else if (Focused)
                {
                    State = PushButtonState.Default;
                }
                else
                {
                    State = PushButtonState.Normal;
                }
            }
        }



        /// <devdoc>
        ///      This is the collection editor's default implementation of a
        ///      collection form.
        /// </devdoc>
        private class CollectionEditorCollectionForm : CollectionForm
        {

            private const int TEXT_INDENT = 1;
            private const int PAINT_WIDTH = 20;
            private const int PAINT_INDENT = 26;
            private static readonly double s_lOG10 = Math.Log(10);

            // Manipulation of the collection.
            //
            private ArrayList _createdItems;
            private ArrayList _removedItems;
            private ArrayList _originalItems;

            // Calling Editor
            private readonly CollectionEditor _editor;

            // Dialog UI
            //
            private FilterListBox _listbox;
            private SplitButton _addButton;
            private Button _removeButton;
            private Button _cancelButton;
            private Button _okButton;
            private Button _downButton;
            private Button _upButton;
            private PropertyGrid _propertyBrowser;
            private Label _membersLabel;
            private Label _propertiesLabel;
            private readonly ContextMenuStrip _addDownMenu;
            private TableLayoutPanel _okCancelTableLayoutPanel;
            private TableLayoutPanel _overArchingTableLayoutPanel;
            private TableLayoutPanel _addRemoveTableLayoutPanel;

            // Prevent flicker when switching selection
            private int _suspendEnabledCount = 0;

            // our flag for if something changed
            //
            private bool _dirty;

            public CollectionEditorCollectionForm(CollectionEditor editor) : base(editor)
            {
                _editor = editor;
                InitializeComponent();
                if (DpiHelper.IsScalingRequired)
                {
                    DpiHelper.ScaleButtonImageLogicalToDevice(_downButton);
                    DpiHelper.ScaleButtonImageLogicalToDevice(_upButton);
                }
                Text = SR.GetResourceString(SR.CollectionEditorCaption, CollectionItemType.Name);

                HookEvents();



                Type[] newItemTypes = NewItemTypes;
                if (newItemTypes.Length > 1)
                {
                    EventHandler addDownMenuClick = new EventHandler(AddDownMenu_click);
                    _addButton.ShowSplit = true;
                    _addDownMenu = new ContextMenuStrip();
                    _addButton.ContextMenuStrip = _addDownMenu;
                    for (int i = 0; i < newItemTypes.Length; i++)
                    {
                        _addDownMenu.Items.Add(new TypeMenuItem(newItemTypes[i], addDownMenuClick));
                    }
                }

                AdjustListBoxItemHeight();
            }

            private bool IsImmutable
            {
                get
                {
                    foreach (ListItem item in _listbox.SelectedItems)
                    {
                        Type type = item.Value.GetType();

                        // The type is considered immutable if the converter is defined as requiring a
                        // create instance or all the properties are read-only.
                        if (!TypeDescriptor.GetConverter(type).GetCreateInstanceSupported())
                        {
                            foreach (PropertyDescriptor p in TypeDescriptor.GetProperties(type))
                            {
                                if (!p.IsReadOnly)
                                {
                                    return false;
                                }
                            }
                        }
                    }

                    return true;
                }
            }

            /// <devdoc>
            ///      Adds a new element to the collection.
            /// </devdoc>
            private void AddButton_click(object sender, EventArgs e)
            {
                PerformAdd();
            }

            /// <devdoc>
            ///      Processes a click of the drop down type menu.  This creates a 
            ///      new instance.
            /// </devdoc>
            private void AddDownMenu_click(object sender, EventArgs e)
            {
                if (sender is TypeMenuItem typeMenuItem)
                {
                    CreateAndAddInstance(typeMenuItem.ItemType);
                }
            }
            
            /// <devdoc>
            ///      This Function adds the individual objects to the ListBox.
            /// </devdoc>
            private void AddItems(IList instances)
            {

                if (_createdItems == null)
                {
                    _createdItems = new ArrayList();
                }

                _listbox.BeginUpdate();
                try
                {
                    foreach (object instance in instances)
                    {
                        if (instance != null)
                        {
                            _dirty = true;
                            _createdItems.Add(instance);
                            ListItem created = new ListItem(_editor, instance);
                            _listbox.Items.Add(created);
                        }
                    }
                }
                finally
                {
                    _listbox.EndUpdate();
                }

                if (instances.Count == 1)
                {
                    // optimize for the case where we just added one thing... 
                    UpdateItemWidths(_listbox.Items[_listbox.Items.Count - 1] as ListItem);
                }
                else
                {
                    // othewise go through the entire list
                    UpdateItemWidths(null);
                }

                // Select the last item
                //
                SuspendEnabledUpdates();
                try
                {
                    _listbox.ClearSelected();
                    _listbox.SelectedIndex = _listbox.Items.Count - 1;

                    object[] items = new object[_listbox.Items.Count];
                    for (int i = 0; i < items.Length; i++)
                    {
                        items[i] = ((ListItem)_listbox.Items[i]).Value;
                    }
                    Items = items;

                    //fringe case -- someone changes the edit value which resets the selindex, we should keep the new index.
                    if (_listbox.Items.Count > 0 && _listbox.SelectedIndex != _listbox.Items.Count - 1)
                    {
                        _listbox.ClearSelected();
                        _listbox.SelectedIndex = _listbox.Items.Count - 1;
                    }
                }

                finally
                {
                    ResumeEnabledUpdates(true);
                }
            }

            private void AdjustListBoxItemHeight()
            {
                _listbox.ItemHeight = Font.Height + SystemInformation.BorderSize.Width * 2;
            }

            /// <devdoc>
            ///     Determines whether removal of a specific list item should be permitted.
            ///     Used to determine enabled/disabled state of the Remove (X) button.
            ///     Items added after editor was opened may always be removed.
            ///     Items that existed before editor was opened require a call to CanRemoveInstance.
            /// </devdoc>
            private bool AllowRemoveInstance(object value)
            {
                if (_createdItems != null && _createdItems.Contains(value))
                {
                    return true;
                }
                else
                {
                    return CanRemoveInstance(value);
                }
            }

            private int CalcItemWidth(Graphics g, ListItem item)
            {
                int c = _listbox.Items.Count;
                if (c < 2)
                {
                    c = 2;  //for c-1 should be greater than zero.
                }

                SizeF sizeW = g.MeasureString(c.ToString(CultureInfo.CurrentCulture), _listbox.Font);

                int charactersInNumber = ((int)(Math.Log((double)(c - 1)) / s_lOG10) + 1);
                int w = 4 + charactersInNumber * (Font.Height / 2);

                w = Math.Max(w, (int)Math.Ceiling(sizeW.Width));
                w += SystemInformation.BorderSize.Width * 4;

                SizeF size = g.MeasureString(GetDisplayText(item), _listbox.Font);
                int pic = 0;
                if (item.Editor != null && item.Editor.GetPaintValueSupported())
                {
                    pic = PAINT_WIDTH + TEXT_INDENT;
                }
                return (int)Math.Ceiling(size.Width) + w + pic + SystemInformation.BorderSize.Width * 4;
            }

            /// <devdoc>
            ///      Aborts changes made in the editor.
            /// </devdoc>
            [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
            private void CancelButton_click(object sender, EventArgs e)
            {
                try
                {

                    _editor.CancelChanges();

                    if (!CollectionEditable || !_dirty)
                    {
                        return;
                    }

                    _dirty = false;
                    _listbox.Items.Clear();

                    if (_createdItems != null)
                    {
                        object[] items = _createdItems.ToArray();
                        if (items.Length > 0 && items[0] is IComponent && ((IComponent)items[0]).Site != null)
                        {
                            // here we bail now because we don't want to do the "undo" manually,
                            // we're part of a trasaction, we've added item, the rollback will be
                            // handled by the undo engine because the component in the collection are sited
                            // doing it here kills perfs because the undo of the transaction has to rollback the remove and then
                            // rollback the add. This is useless and is only needed for non sited component or other classes
                            return;
                        }
                        for (int i = 0; i < items.Length; i++)
                        {
                            DestroyInstance(items[i]);
                        }
                        _createdItems.Clear();
                    }
                    if (_removedItems != null)
                    {
                        _removedItems.Clear();
                    }


                    // Restore the original contents. Because objects get parented during CreateAndAddInstance, the underlying collection
                    // gets changed during add, but not other operations. Not all consumers of this dialog can roll back every single change,
                    // but this will at least roll back the additions, removals and reordering. See ASURT #85470.
                    if (_originalItems != null && (_originalItems.Count > 0))
                    {
                        object[] items = new object[_originalItems.Count];
                        for (int i = 0; i < _originalItems.Count; i++)
                        {
                            items[i] = _originalItems[i];
                        }
                        Items = items;
                        _originalItems.Clear();
                    }
                    else
                    {
                        Items = new object[0];
                    }

                }
                catch (Exception ex)
                {
                    DialogResult = DialogResult.None;
                    DisplayError(ex);
                }
            }
            
            /// <devdoc>
            ///      Performs a create instance and then adds the instance to
            ///      the list box.
            /// </devdoc>
            [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
            private void CreateAndAddInstance(Type type)
            {
                try
                {
                    object instance = CreateInstance(type);
                    IList multipleInstance = _editor.GetObjectsFromInstance(instance);

                    if (multipleInstance != null)
                    {
                        AddItems(multipleInstance);
                    }
                }
                catch (Exception e)
                {
                    DisplayError(e);
                }
            }

            /// <devdoc>
            ///      Moves the selected item down one.
            /// </devdoc>
            private void DownButton_click(object sender, EventArgs e)
            {
                try
                {
                    SuspendEnabledUpdates();
                    _dirty = true;
                    int index = _listbox.SelectedIndex;
                    if (index == _listbox.Items.Count - 1)
                        return;
                    int ti = _listbox.TopIndex;
                    object itemMove = _listbox.Items[index];
                    _listbox.Items[index] = _listbox.Items[index + 1];
                    _listbox.Items[index + 1] = itemMove;

                    if (ti < _listbox.Items.Count - 1)
                        _listbox.TopIndex = ti + 1;

                    _listbox.ClearSelected();
                    _listbox.SelectedIndex = index + 1;

                    // enabling/disabling the buttons has moved the focus to the OK button, move it back to the sender
                    Control ctrlSender = (Control)sender;

                    if (ctrlSender.Enabled)
                    {
                        ctrlSender.Focus();
                    }
                }
                finally
                {

                    ResumeEnabledUpdates(true);
                }
            }

            private void CollectionEditor_HelpButtonClicked(object sender, CancelEventArgs e)
            {
                e.Cancel = true;
                _editor.ShowHelp();
            }

            private void Form_HelpRequested(object sender, HelpEventArgs e)
            {
                _editor.ShowHelp();
            }

            /// <devdoc>
            ///     Retrieves the display text for the given list item (if any). The item determines its own display text
            ///     through its ToString() method, which delegates to the GetDisplayText() override on the parent CollectionEditor.
            ///     This means in theory that the text can change at any time (ie. its not fixed when the item is added to the list).
            ///     The item returns its display text through ToString() so that the same text will be reported to Accessibility clients.
            /// </devdoc>
            private string GetDisplayText(ListItem item)
            {
                return (item == null) ? string.Empty : item.ToString();
            }

            private void HookEvents()
            {
                _listbox.KeyDown += new KeyEventHandler(Listbox_keyDown);
                _listbox.DrawItem += new DrawItemEventHandler(Listbox_drawItem);
                _listbox.SelectedIndexChanged += new EventHandler(Listbox_selectedIndexChanged);
                _listbox.HandleCreated += new EventHandler(Listbox_handleCreated);
                _upButton.Click += new EventHandler(UpButton_click);
                _downButton.Click += new EventHandler(DownButton_click);
                _propertyBrowser.PropertyValueChanged += new PropertyValueChangedEventHandler(PropertyGrid_propertyValueChanged);
                _addButton.Click += new EventHandler(AddButton_click);
                _removeButton.Click += new EventHandler(RemoveButton_click);
                _okButton.Click += new EventHandler(OKButton_click);
                _cancelButton.Click += new EventHandler(CancelButton_click);
                HelpButtonClicked += new CancelEventHandler(CollectionEditor_HelpButtonClicked);
                HelpRequested += new HelpEventHandler(Form_HelpRequested);
                Shown += new EventHandler(Form_Shown);
            }

            private void InitializeComponent()
            {
                ComponentResourceManager resources = new ComponentResourceManager(typeof(CollectionEditor));
                _membersLabel = new Label();
                _listbox = new FilterListBox();
                _upButton = new Button();
                _downButton = new Button();
                _propertiesLabel = new Label();
                _propertyBrowser = new PropertyGrid();
                _addButton = new SplitButton();
                _removeButton = new Button();
                _okButton = new Button();
                _cancelButton = new Button();
                _okCancelTableLayoutPanel = new TableLayoutPanel();
                _overArchingTableLayoutPanel = new TableLayoutPanel();
                _addRemoveTableLayoutPanel = new TableLayoutPanel();
                _okCancelTableLayoutPanel.SuspendLayout();
                _overArchingTableLayoutPanel.SuspendLayout();
                _addRemoveTableLayoutPanel.SuspendLayout();
                SuspendLayout();
                // 
                // membersLabel
                // 
                resources.ApplyResources(_membersLabel, "membersLabel");
                _membersLabel.Margin = new Padding(0, 0, 3, 3);
                _membersLabel.Name = "membersLabel";
                // 
                // listbox
                // 
                resources.ApplyResources(_listbox, "listbox");
                _listbox.SelectionMode = (CanSelectMultipleInstances() ? SelectionMode.MultiExtended : SelectionMode.One);
                _listbox.DrawMode = DrawMode.OwnerDrawFixed;
                _listbox.FormattingEnabled = true;
                _listbox.Margin = new Padding(0, 3, 3, 3);
                _listbox.Name = "listbox";
                _overArchingTableLayoutPanel.SetRowSpan(_listbox, 2);
                // 
                // upButton
                // 
                resources.ApplyResources(_upButton, "upButton");
                _upButton.Name = "upButton";
                // 
                // downButton
                // 
                resources.ApplyResources(_downButton, "downButton");
                _downButton.Name = "downButton";
                // 
                // propertiesLabel
                // 
                resources.ApplyResources(_propertiesLabel, "propertiesLabel");
                _propertiesLabel.AutoEllipsis = true;
                _propertiesLabel.Margin = new Padding(0, 0, 3, 3);
                _propertiesLabel.Name = "propertiesLabel";
                // 
                // propertyBrowser
                // 
                resources.ApplyResources(_propertyBrowser, "propertyBrowser");
                _propertyBrowser.CommandsVisibleIfAvailable = false;
                _propertyBrowser.Margin = new Padding(3, 3, 0, 3);
                _propertyBrowser.Name = "propertyBrowser";
                _overArchingTableLayoutPanel.SetRowSpan(_propertyBrowser, 3);
                // 
                // addButton
                // 
                resources.ApplyResources(_addButton, "addButton");
                _addButton.Margin = new Padding(0, 3, 3, 3);
                _addButton.Name = "addButton";
                // 
                // removeButton
                // 
                resources.ApplyResources(_removeButton, "removeButton");
                _removeButton.Margin = new Padding(3, 3, 0, 3);
                _removeButton.Name = "removeButton";
                // 
                // okButton
                // 
                resources.ApplyResources(_okButton, "okButton");
                _okButton.DialogResult = DialogResult.OK;
                _okButton.Margin = new Padding(0, 3, 3, 0);
                _okButton.Name = "okButton";
                // 
                // cancelButton
                // 
                resources.ApplyResources(_cancelButton, "cancelButton");
                _cancelButton.DialogResult = DialogResult.Cancel;
                _cancelButton.Margin = new Padding(3, 3, 0, 0);
                _cancelButton.Name = "cancelButton";
                // 
                // okCancelTableLayoutPanel
                // 
                resources.ApplyResources(_okCancelTableLayoutPanel, "okCancelTableLayoutPanel");
                _overArchingTableLayoutPanel.SetColumnSpan(_okCancelTableLayoutPanel, 3);
                _okCancelTableLayoutPanel.Controls.Add(_okButton, 0, 0);
                _okCancelTableLayoutPanel.Controls.Add(_cancelButton, 1, 0);
                _okCancelTableLayoutPanel.Margin = new Padding(3, 3, 0, 0);
                _okCancelTableLayoutPanel.Name = "okCancelTableLayoutPanel";
                // 
                // overArchingTableLayoutPanel
                // 
                resources.ApplyResources(_overArchingTableLayoutPanel, "overArchingTableLayoutPanel");
                _overArchingTableLayoutPanel.Controls.Add(_downButton, 1, 2);
                _overArchingTableLayoutPanel.Controls.Add(_addRemoveTableLayoutPanel, 0, 3);
                _overArchingTableLayoutPanel.Controls.Add(_propertiesLabel, 2, 0);
                _overArchingTableLayoutPanel.Controls.Add(_membersLabel, 0, 0);
                _overArchingTableLayoutPanel.Controls.Add(_listbox, 0, 1);
                _overArchingTableLayoutPanel.Controls.Add(_propertyBrowser, 2, 1);
                _overArchingTableLayoutPanel.Controls.Add(_okCancelTableLayoutPanel, 0, 4);
                _overArchingTableLayoutPanel.Controls.Add(_upButton, 1, 1);
                _overArchingTableLayoutPanel.Name = "overArchingTableLayoutPanel";
                // 
                // addRemoveTableLayoutPanel
                // 
                resources.ApplyResources(_addRemoveTableLayoutPanel, "addRemoveTableLayoutPanel");
                _addRemoveTableLayoutPanel.Controls.Add(_addButton, 0, 0);
                _addRemoveTableLayoutPanel.Controls.Add(_removeButton, 2, 0);
                _addRemoveTableLayoutPanel.Margin = new Padding(0, 3, 3, 3);
                _addRemoveTableLayoutPanel.Name = "addRemoveTableLayoutPanel";
                // 
                // CollectionEditor
                // 
                AcceptButton = _okButton;
                resources.ApplyResources(this, "$this");
                AutoScaleMode = AutoScaleMode.Font;
                CancelButton = _cancelButton;
                Controls.Add(_overArchingTableLayoutPanel);
                HelpButton = true;
                MaximizeBox = false;
                MinimizeBox = false;
                Name = "CollectionEditor";
                ShowIcon = false;
                ShowInTaskbar = false;
                _okCancelTableLayoutPanel.ResumeLayout(false);
                _okCancelTableLayoutPanel.PerformLayout();
                _overArchingTableLayoutPanel.ResumeLayout(false);
                _overArchingTableLayoutPanel.PerformLayout();
                _addRemoveTableLayoutPanel.ResumeLayout(false);
                _addRemoveTableLayoutPanel.PerformLayout();
                ResumeLayout(false);
            }

            private void UpdateItemWidths(ListItem item)
            {
                // VSWhidbey#384112: Its neither safe nor accurate to perform these width
                // calculations prior to normal listbox handle creation. So we nop in this case now.
                if (!_listbox.IsHandleCreated)
                {
                    return;
                }

                using (Graphics g = _listbox.CreateGraphics())
                {
                    int old = _listbox.HorizontalExtent;

                    if (item != null)
                    {
                        int w = CalcItemWidth(g, item);
                        if (w > old)
                        {
                            _listbox.HorizontalExtent = w;
                        }
                    }
                    else
                    {
                        int max = 0;
                        foreach (ListItem i in _listbox.Items)
                        {
                            int w = CalcItemWidth(g, i);
                            if (w > max)
                            {
                                max = w;
                            }
                        }
                        _listbox.HorizontalExtent = max;
                    }
                }
            }

            /// <devdoc>
            ///     This draws a row of the listbox.
            /// </devdoc>
            private void Listbox_drawItem(object sender, DrawItemEventArgs e)
            {
                if (e.Index != -1)
                {
                    ListItem item = (ListItem)_listbox.Items[e.Index];

                    Graphics g = e.Graphics;

                    int c = _listbox.Items.Count;
                    int maxC = (c > 1) ? c - 1 : c;
                    // We add the +4 is a fudge factor...
                    //
                    SizeF sizeW = g.MeasureString(maxC.ToString(CultureInfo.CurrentCulture), _listbox.Font);

                    int charactersInNumber = ((int)(Math.Log((double)maxC) / s_lOG10) + 1);// Luckily, this is never called if count = 0
                    int w = 4 + charactersInNumber * (Font.Height / 2);

                    w = Math.Max(w, (int)Math.Ceiling(sizeW.Width));
                    w += SystemInformation.BorderSize.Width * 4;

                    Rectangle button = new Rectangle(e.Bounds.X, e.Bounds.Y, w, e.Bounds.Height);

                    ControlPaint.DrawButton(g, button, ButtonState.Normal);
                    button.Inflate(-SystemInformation.BorderSize.Width * 2, -SystemInformation.BorderSize.Height * 2);

                    int offset = w;

                    Color backColor = SystemColors.Window;
                    Color textColor = SystemColors.WindowText;
                    if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                    {
                        backColor = SystemColors.Highlight;
                        textColor = SystemColors.HighlightText;
                    }
                    Rectangle res = new Rectangle(e.Bounds.X + offset, e.Bounds.Y,
                                                  e.Bounds.Width - offset,
                                                  e.Bounds.Height);
                    g.FillRectangle(new SolidBrush(backColor), res);
                    if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
                    {
                        ControlPaint.DrawFocusRectangle(g, res);
                    }
                    offset += 2;

                    if (item.Editor != null && item.Editor.GetPaintValueSupported())
                    {
                        Rectangle baseVar = new Rectangle(e.Bounds.X + offset, e.Bounds.Y + 1, PAINT_WIDTH, e.Bounds.Height - 3);
                        g.DrawRectangle(SystemPens.ControlText, baseVar.X, baseVar.Y, baseVar.Width - 1, baseVar.Height - 1);
                        baseVar.Inflate(-1, -1);
                        item.Editor.PaintValue(item.Value, g, baseVar);
                        offset += PAINT_INDENT + TEXT_INDENT;
                    }

                    StringFormat format = new StringFormat();
                    try
                    {
                        format.Alignment = StringAlignment.Center;
                        g.DrawString(e.Index.ToString(CultureInfo.CurrentCulture), Font, SystemBrushes.ControlText,
                                     new Rectangle(e.Bounds.X, e.Bounds.Y, w, e.Bounds.Height), format);
                    }

                    finally
                    {
                        if (format != null)
                        {
                            format.Dispose();
                        }
                    }

                    Brush textBrush = new SolidBrush(textColor);

                    string itemText = GetDisplayText(item);

                    try
                    {
                        g.DrawString(itemText, Font, textBrush,
                                     new Rectangle(e.Bounds.X + offset, e.Bounds.Y, e.Bounds.Width - offset, e.Bounds.Height));
                    }

                    finally
                    {
                        if (textBrush != null)
                        {
                            textBrush.Dispose();
                        }
                    }

                    // Check to see if we need to change the horizontal extent of the listbox
                    //                                    
                    int width = offset + (int)g.MeasureString(itemText, Font).Width;
                    if (width > e.Bounds.Width && _listbox.HorizontalExtent < width)
                    {
                        _listbox.HorizontalExtent = width;
                    }
                }
            }

            /// <devdoc>
            ///      Handles keypress events for the list box.
            /// </devdoc>
            private void Listbox_keyDown(object sender, KeyEventArgs kevent)
            {
                switch (kevent.KeyData)
                {
                    case Keys.Delete:
                        PerformRemove();
                        break;
                    case Keys.Insert:
                        PerformAdd();
                        break;
                }
            }

            /// <devdoc>
            ///      Event that fires when the selected list box index changes.
            /// </devdoc>
            private void Listbox_selectedIndexChanged(object sender, EventArgs e)
            {
                UpdateEnabled();
            }

            /// <devdoc>
            ///      Event that fires when the list box's window handle is created.
            /// </devdoc>
            private void Listbox_handleCreated(object sender, EventArgs e)
            {
                // VSWhidbey#384112: Since we no longer perform width calculations prior to handle
                // creation now, we need to ensure we do it at least once after handle creation.
                UpdateItemWidths(null);
            }

            /// <devdoc>
            ///      Commits the changes to the editor.
            /// </devdoc>
            [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
            private void OKButton_click(object sender, EventArgs e)
            {
                try
                {

                    if (!_dirty || !CollectionEditable)
                    {
                        _dirty = false;
                        DialogResult = DialogResult.Cancel;
                        return;
                    }

                    // Now apply the changes to the actual value.
                    //
                    if (_dirty)
                    {
                        object[] items = new object[_listbox.Items.Count];
                        for (int i = 0; i < items.Length; i++)
                        {
                            items[i] = ((ListItem)_listbox.Items[i]).Value;
                        }

                        Items = items;
                    }


                    // Now destroy any existing items we had.
                    //
                    if (_removedItems != null && _dirty)
                    {
                        object[] deadItems = _removedItems.ToArray();

                        for (int i = 0; i < deadItems.Length; i++)
                        {
                            DestroyInstance(deadItems[i]);
                        }
                        _removedItems.Clear();
                    }
                    if (_createdItems != null)
                    {
                        _createdItems.Clear();
                    }

                    if (_originalItems != null)
                    {
                        _originalItems.Clear();
                    }

                    _listbox.Items.Clear();
                    _dirty = false;
                }
                catch (Exception ex)
                {
                    DialogResult = DialogResult.None;
                    DisplayError(ex);
                }
            }

            /// <devdoc>
            /// reflect any change events to the instance object
            /// </devdoc>
            private void OnComponentChanged(object sender, ComponentChangedEventArgs e)
            {

                // see if this is any of the items in our list...this can happen if
                // we launched a child editor
                if (!_dirty && _originalItems != null)
                {
                    foreach (object item in _originalItems)
                    {
                        if (item == e.Component)
                        {
                            _dirty = true;
                            break;
                        }
                    }
                }

            }

            /// <devdoc>
            ///      This is called when the value property in the CollectionForm has changed.
            ///      In it you should update your user interface to reflect the current value.
            /// </devdoc>
            protected override void OnEditValueChanged()
            {
                if (!Visible)
                {
                    return;
                }

                // Remember these contents for cancellation
                if (_originalItems == null)
                {
                    _originalItems = new ArrayList();
                }
                _originalItems.Clear();

                // Now update the list box.
                //
                _listbox.Items.Clear();
                _propertyBrowser.Site = new PropertyGridSite(Context, _propertyBrowser);
                if (EditValue != null)
                {
                    SuspendEnabledUpdates();
                    try
                    {
                        object[] items = Items;
                        for (int i = 0; i < items.Length; i++)
                        {
                            _listbox.Items.Add(new ListItem(_editor, items[i]));
                            _originalItems.Add(items[i]);
                        }
                        if (_listbox.Items.Count > 0)
                        {
                            _listbox.SelectedIndex = 0;
                        }
                    }
                    finally
                    {
                        ResumeEnabledUpdates(true);
                    }
                }
                else
                {
                    UpdateEnabled();
                }

                AdjustListBoxItemHeight();
                UpdateItemWidths(null);

            }

            protected override void OnFontChanged(EventArgs e)
            {
                base.OnFontChanged(e);
                AdjustListBoxItemHeight();
            }

            /// <devdoc>
            ///     Performs the actual add of new items.  This is invoked by the
            ///     add button as well as the insert key on the list box.
            /// </devdoc>
            private void PerformAdd()
            {
                CreateAndAddInstance(NewItemTypes[0]);
            }

            /// <devdoc>
            ///     Performs a remove by deleting all items currently selected in
            ///     the list box.  This is called by the delete button as well as
            ///     the delete key on the list box.
            /// </devdoc>
            private void PerformRemove()
            {
                int index = _listbox.SelectedIndex;

                if (index != -1)
                {
                    SuspendEnabledUpdates();
                    try
                    {

                        // single object selected or multiple ?
                        if (_listbox.SelectedItems.Count > 1)
                        {
                            ArrayList toBeDeleted = new ArrayList(_listbox.SelectedItems);
                            foreach (ListItem item in toBeDeleted)
                            {
                                RemoveInternal(item);
                            }
                        }
                        else
                        {
                            RemoveInternal((ListItem)_listbox.SelectedItem);
                        }
                        // set the new selected index
                        if (index < _listbox.Items.Count)
                        {
                            _listbox.SelectedIndex = index;
                        }
                        else if (_listbox.Items.Count > 0)
                        {
                            _listbox.SelectedIndex = _listbox.Items.Count - 1;
                        }
                    }
                    finally
                    {
                        ResumeEnabledUpdates(true);
                    }
                }
            }

            /// <devdoc>
            ///      When something in the properties window changes, we update pertinent text here.
            /// </devdoc>
            private void PropertyGrid_propertyValueChanged(object sender, PropertyValueChangedEventArgs e)
            {

                _dirty = true;

                // Refresh selected listbox item so that it picks up any name change
                SuspendEnabledUpdates();
                try
                {
                    int selectedItem = _listbox.SelectedIndex;
                    if (selectedItem >= 0)
                    {
                        _listbox.RefreshItem(_listbox.SelectedIndex);
                    }
                }
                finally
                {
                    ResumeEnabledUpdates(false);
                }

                // if a property changes, invalidate the grid in case
                // it affects the item's name.
                UpdateItemWidths(null);
                _listbox.Invalidate();

                // also update the string above the grid.
                _propertiesLabel.Text = SR.GetResourceString(SR.CollectionEditorProperties, GetDisplayText((ListItem)_listbox.SelectedItem));
            }

            /// <devdoc>
            ///      Used to actually remove the items, one by one.
            /// </devdoc>
            [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
            private void RemoveInternal(ListItem item)
            {
                if (item != null)
                {

                    _editor.OnItemRemoving(item.Value);

                    _dirty = true;
                    //ListItem item = (ListItem)listbox.Items[index];

                    if (_createdItems != null && _createdItems.Contains(item.Value))
                    {
                        DestroyInstance(item.Value);
                        _createdItems.Remove(item.Value);
                        _listbox.Items.Remove(item);
                    }
                    else
                    {
                        try
                        {
                            if (CanRemoveInstance(item.Value))
                            {
                                if (_removedItems == null)
                                {
                                    _removedItems = new ArrayList();
                                }
                                _removedItems.Add(item.Value);
                                _listbox.Items.Remove(item);
                            }
                            else
                            {
                                throw new Exception(SR.GetResourceString(SR.CollectionEditorCantRemoveItem, GetDisplayText(item)));
                            }
                        }
                        catch (Exception ex)
                        {
                            DisplayError(ex);
                        }
                    }
                    // othewise go through the entire list
                    UpdateItemWidths(null);


                }
            }
            
            /// <devdoc>
            ///      Removes the selected item.
            /// </devdoc>
            private void RemoveButton_click(object sender, EventArgs e)
            {
                PerformRemove();

                // enabling/disabling the buttons has moved the focus to the OK button, move it back to the sender
                Control ctrlSender = (Control)sender;
                if (ctrlSender.Enabled)
                {
                    ctrlSender.Focus();
                }
            }

            /// <devdoc>
            /// used to prevent flicker when playing with the list box selection
            /// call resume when done.  Calls to UpdateEnabled will return silently until Resume is called
            /// </devdoc>
            private void ResumeEnabledUpdates(bool updateNow)
            {
                _suspendEnabledCount--;

                Debug.Assert(_suspendEnabledCount >= 0, "Mismatch suspend/resume enabled");

                if (updateNow)
                {
                    UpdateEnabled();
                }
                else
                {
                    BeginInvoke(new MethodInvoker(UpdateEnabled));
                }
            }
            /// <devdoc>
            /// used to prevent flicker when playing with the list box selection
            /// call resume when done.  Calls to UpdateEnabled will return silently until Resume is called
            /// </devdoc>
            private void SuspendEnabledUpdates()
            {
                _suspendEnabledCount++;
            }
            
            /// <devdoc>
            ///       Called to show the dialog via the IWindowsFormsEditorService 
            /// </devdoc>
            protected internal override DialogResult ShowEditorDialog(IWindowsFormsEditorService edSvc)
            {
                IComponentChangeService cs = null;
                DialogResult result = DialogResult.OK;
                try
                {

                    cs = (IComponentChangeService)_editor.Context.GetService(typeof(IComponentChangeService));

                    if (cs != null)
                    {
                        cs.ComponentChanged += new ComponentChangedEventHandler(OnComponentChanged);
                    }

                    // This is cached across requests, so reset the initial focus.
                    ActiveControl = _listbox;
                    //SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(this.OnSysColorChange);
                    result = base.ShowEditorDialog(edSvc);
                    //SystemEvents.UserPreferenceChanged -= new UserPreferenceChangedEventHandler(this.OnSysColorChange);
                }
                finally
                {

                    if (cs != null)
                    {
                        cs.ComponentChanged -= new ComponentChangedEventHandler(OnComponentChanged);
                    }
                }
                return result;
            }

            /// <devdoc>
            ///      Moves an item up one in the list box.
            /// </devdoc>
            private void UpButton_click(object sender, EventArgs e)
            {
                int index = _listbox.SelectedIndex;
                if (index == 0)
                    return;

                _dirty = true;
                try
                {
                    SuspendEnabledUpdates();
                    int ti = _listbox.TopIndex;
                    object itemMove = _listbox.Items[index];
                    _listbox.Items[index] = _listbox.Items[index - 1];
                    _listbox.Items[index - 1] = itemMove;

                    if (ti > 0)
                        _listbox.TopIndex = ti - 1;

                    _listbox.ClearSelected();
                    _listbox.SelectedIndex = index - 1;

                    // enabling/disabling the buttons has moved the focus to the OK button, move it back to the sender
                    Control ctrlSender = (Control)sender;

                    if (ctrlSender.Enabled)
                    {
                        ctrlSender.Focus();
                    }
                }
                finally
                {

                    ResumeEnabledUpdates(true);
                }

            }

            /// <devdoc>
            ///      Updates the set of enabled buttons.
            /// </devdoc>
            private void UpdateEnabled()
            {
                if (_suspendEnabledCount > 0)
                {
                    // We're in the midst of a suspend/resume block  Resume should call us back.
                    return;
                }

                bool editEnabled = (_listbox.SelectedItem != null) && CollectionEditable;
                _removeButton.Enabled = editEnabled && AllowRemoveInstance(((ListItem)_listbox.SelectedItem).Value);
                _upButton.Enabled = editEnabled && _listbox.Items.Count > 1;
                _downButton.Enabled = editEnabled && _listbox.Items.Count > 1;
                _propertyBrowser.Enabled = editEnabled;
                _addButton.Enabled = CollectionEditable;

                if (_listbox.SelectedItem != null)
                {
                    object[] items;

                    // If we are to create new instances from the items, then we must wrap them in an outer object.
                    // otherwise, the user will be presented with a batch of read only properties, which isn't terribly
                    // useful.
                    //
                    if (IsImmutable)
                    {
                        items = new object[] { new SelectionWrapper(CollectionType, CollectionItemType, _listbox, _listbox.SelectedItems) };
                    }
                    else
                    {
                        items = new object[_listbox.SelectedItems.Count];
                        for (int i = 0; i < items.Length; i++)
                        {
                            items[i] = ((ListItem)_listbox.SelectedItems[i]).Value;
                        }
                    }

                    int selectedItemCount = _listbox.SelectedItems.Count;
                    if ((selectedItemCount == 1) || (selectedItemCount == -1))
                    {
                        // handle both single select listboxes and a single item selected in a multi-select listbox
                        _propertiesLabel.Text = SR.GetResourceString(SR.CollectionEditorProperties, GetDisplayText((ListItem)_listbox.SelectedItem));
                    }
                    else
                    {
                        _propertiesLabel.Text = SR.GetResourceString(SR.CollectionEditorPropertiesMultiSelect);
                    }

                    if (_editor.IsAnyObjectInheritedReadOnly(items))
                    {
                        _propertyBrowser.SelectedObjects = null;
                        _propertyBrowser.Enabled = false;
                        _removeButton.Enabled = false;
                        _upButton.Enabled = false;
                        _downButton.Enabled = false;
                        _propertiesLabel.Text = SR.GetResourceString(SR.CollectionEditorInheritedReadOnlySelection);
                    }
                    else
                    {
                        _propertyBrowser.Enabled = true;
                        _propertyBrowser.SelectedObjects = items;
                    }
                }
                else
                {
                    _propertiesLabel.Text = SR.GetResourceString(SR.CollectionEditorPropertiesNone);
                    _propertyBrowser.SelectedObject = null;
                }
            }

            /// <devdoc>
            ///      When the form is first shown, update controls due to the edit value changes which happened when the form is invisible.
            /// </devdoc>
            private void Form_Shown(object sender, EventArgs e)
            {
                OnEditValueChanged();
            }

            /// <devdoc>
            ///     This class implements a custom type descriptor that is used to provide properties for the set of
            ///     selected items in the collection editor.  It provides a single property that is equivalent
            ///     to the editor's collection item type.
            /// </devdoc>
            private class SelectionWrapper : PropertyDescriptor, ICustomTypeDescriptor
            {
                private readonly Type _collectionType;
                private readonly Type _collectionItemType;
                private readonly Control _control;
                private readonly ICollection _collection;
                private readonly PropertyDescriptorCollection _properties;
                private object _value;

                public SelectionWrapper(Type collectionType, Type collectionItemType, Control control, ICollection collection) :
                base("Value",
                     new Attribute[] { new CategoryAttribute(collectionItemType.Name) }
                    )
                {
                    _collectionType = collectionType;
                    _collectionItemType = collectionItemType;
                    _control = control;
                    _collection = collection;
                    _properties = new PropertyDescriptorCollection(new PropertyDescriptor[] { this });

                    Debug.Assert(collection.Count > 0, "We should only be wrapped if there is a selection");
                    _value = this;

                    // In a multiselect case, see if the values are different.  If so,
                    // NULL our value to represent indeterminate.
                    //
                    foreach (ListItem li in collection)
                    {
                        if (_value == this)
                        {
                            _value = li.Value;
                        }
                        else
                        {
                            object nextValue = li.Value;
                            if (_value != null)
                            {
                                if (nextValue == null)
                                {
                                    _value = null;
                                    break;
                                }
                                else
                                {
                                    if (!_value.Equals(nextValue))
                                    {
                                        _value = null;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (nextValue != null)
                                {
                                    _value = null;
                                    break;
                                }
                            }
                        }
                    }
                }

                /// <devdoc>
                ///       When overridden in a derived class, gets the type of the component this property is bound to.
                /// </devdoc>
                public override Type ComponentType
                {
                    get
                    {
                        return _collectionType;
                    }
                }

                /// <devdoc>
                ///       When overridden in a derived class, gets a value indicating whether this property is read-only.
                /// </devdoc>
                public override bool IsReadOnly
                {
                    get
                    {
                        return false;
                    }
                }

                /// <devdoc>
                ///       When overridden in a derived class, gets the type of the property.
                /// </devdoc>
                public override Type PropertyType
                {
                    get
                    {
                        return _collectionItemType;
                    }
                }

                /// <devdoc>
                ///    When overridden in a derived class, indicates whether resetting the <paramref name="component "/>will change the value of the
                ///    <paramref name="component"/>.
                /// </devdoc>
                public override bool CanResetValue(object component)
                {
                    return false;
                }

                /// <devdoc>
                ///       When overridden in a derived class, gets the current value of the property on a component.
                /// </devdoc>
                public override object GetValue(object component)
                {
                    return _value;
                }

                /// <devdoc>
                ///       When overridden in a derived class, resets the value for this property of the component.
                /// </devdoc>
                public override void ResetValue(object component)
                {
                }

                /// <devdoc>
                ///       When overridden in a derived class, sets the value of the component to a different value.
                /// </devdoc>
                public override void SetValue(object component, object value)
                {
                    _value = value;

                    foreach (ListItem li in _collection)
                    {
                        li.Value = value;
                    }
                    _control.Invalidate();
                    OnValueChanged(component, EventArgs.Empty);
                }

                /// <devdoc>
                ///       When overridden in a derived class, indicates whether the value of this property needs to be persisted.
                /// </devdoc>
                public override bool ShouldSerializeValue(object component)
                {
                    return false;
                }

                /// <devdoc>
                ///     Retrieves an array of member attributes for the given object.
                /// </devdoc>
                AttributeCollection ICustomTypeDescriptor.GetAttributes()
                {
                    return TypeDescriptor.GetAttributes(_collectionItemType);
                }

                /// <devdoc>
                ///     Retrieves the class name for this object.  If null is returned,
                ///     the type name is used.
                /// </devdoc>
                string ICustomTypeDescriptor.GetClassName()
                {
                    return _collectionItemType.Name;
                }

                /// <devdoc>
                ///     Retrieves the name for this object.  If null is returned, the default is used.
                /// </devdoc>
                string ICustomTypeDescriptor.GetComponentName()
                {
                    return null;
                }

                /// <devdoc>
                ///      Retrieves the type converter for this object.
                /// </devdoc>
                TypeConverter ICustomTypeDescriptor.GetConverter()
                {
                    return null;
                }

                /// <devdoc>
                ///     Retrieves the default event.
                /// </devdoc>
                EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
                {
                    return null;
                }

                /// <devdoc>
                ///     Retrieves the default property.
                /// </devdoc>
                PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
                {
                    return this;
                }

                /// <devdoc>
                ///      Retrieves the an editor for this object.
                /// </devdoc>
                object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
                {
                    return null;
                }

                /// <devdoc>
                ///     Retrieves an array of events that the given component instance
                ///     provides.  This may differ from the set of events the class
                ///     provides.  If the component is sited, the site may add or remove
                ///     additional events.
                /// </devdoc>
                EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
                {
                    return EventDescriptorCollection.Empty;
                }

                /// <devdoc>
                ///     Retrieves an array of events that the given component instance
                ///     provides.  This may differ from the set of events the class
                ///     provides.  If the component is sited, the site may add or remove
                ///     additional events.  The returned array of events will be
                ///     filtered by the given set of attributes.
                /// </devdoc>
                EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
                {
                    return EventDescriptorCollection.Empty;
                }

                /// <devdoc>
                ///     Retrieves an array of properties that the given component instance
                ///     provides.  This may differ from the set of properties the class
                ///     provides.  If the component is sited, the site may add or remove
                ///     additional properties.
                /// </devdoc>
                PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
                {
                    return _properties;
                }

                /// <devdoc>
                ///     Retrieves an array of properties that the given component instance
                ///     provides.  This may differ from the set of properties the class
                ///     provides.  If the component is sited, the site may add or remove
                ///     additional properties.  The returned array of properties will be
                ///     filtered by the given set of attributes.
                /// </devdoc>
                PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
                {
                    return _properties;
                }

                /// <devdoc>
                ///     Retrieves the object that directly depends on this value being edited.  This is
                ///     generally the object that is required for the PropertyDescriptor's GetValue and SetValue
                ///     methods.  If 'null' is passed for the PropertyDescriptor, the ICustomComponent
                ///     descripotor implemementation should return the default object, that is the main
                ///     object that exposes the properties and attributes,
                /// </devdoc>
                object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
                {
                    return this;
                }
            }

            /// <devdoc>
            ///      ListItem class.  This is a single entry in our list box.  It contains the value we're editing
            ///      as well as accessors for the type converter and UI editor.
            /// </devdoc>
            private class ListItem
            {
                private object _value;
                private object _uiTypeEditor;
                private readonly CollectionEditor _parentCollectionEditor;

                public ListItem(CollectionEditor parentCollectionEditor, object value)
                {
                    _value = value;
                    _parentCollectionEditor = parentCollectionEditor;
                }

                public override string ToString()
                {
                    return _parentCollectionEditor.GetDisplayText(_value);
                }

                public UITypeEditor Editor
                {
                    get
                    {
                        if (_uiTypeEditor == null)
                        {
                            _uiTypeEditor = TypeDescriptor.GetEditor(_value, typeof(UITypeEditor));
                            if (_uiTypeEditor == null)
                            {
                                _uiTypeEditor = this;
                            }
                        }

                        if (_uiTypeEditor != this)
                        {
                            return (UITypeEditor)_uiTypeEditor;
                        }

                        return null;
                    }
                }

                public object Value
                {
                    get
                    {
                        return _value;
                    }
                    set
                    {
                        _uiTypeEditor = null;
                        _value = value;
                    }
                }
            }

            /// <devdoc>
            ///      Menu items we attach to the drop down menu if there are multiple
            ///      types the collection editor can create.
            /// </devdoc>
            private class TypeMenuItem : ToolStripMenuItem
            {
                readonly Type _itemType;

                public TypeMenuItem(Type itemType, EventHandler handler) :
                base(itemType.Name, null, handler)
                {
                    _itemType = itemType;
                }

                public Type ItemType
                {
                    get
                    {
                        return _itemType;
                    }
                }
            }
        }
        
        /// <devdoc>
        ///      List box filled with ListItem objects representing the collection.
        /// </devdoc>
        internal class FilterListBox : ListBox
        {


            private PropertyGrid _grid;
            private Message _lastKeyDown;

            private PropertyGrid PropertyGrid
            {
                get
                {
                    if (_grid == null)
                    {
                        foreach (Control c in Parent.Controls)
                        {
                            if (c is PropertyGrid)
                            {
                                _grid = (PropertyGrid)c;
                                break;
                            }
                        }
                    }
                    return _grid;
                }

            }

            // Expose the protected RefreshItem() method so that CollectionEditor can use it
            public new void RefreshItem(int index)
            {
                base.RefreshItem(index);
            }

            protected override void WndProc(ref Message m)
            {
                switch (m.Msg)
                {
                    case NativeMethods.WM_KEYDOWN:
                        _lastKeyDown = m;

                        // the first thing the ime does on a key it cares about is send a VK_PROCESSKEY, so we use that to sling focus to the grid.
                        if (unchecked((int)(long)m.WParam) == NativeMethods.VK_PROCESSKEY)
                        {
                            if (PropertyGrid != null)
                            {
                                PropertyGrid.Focus();
                                UnsafeNativeMethods.SetFocus(new HandleRef(PropertyGrid, PropertyGrid.Handle));
                                Application.DoEvents();
                            }
                            else
                            {
                                break;
                            }

                            if (PropertyGrid.Focused || PropertyGrid.ContainsFocus)
                            {
                                // recreate the keystroke to the newly activated window
                                NativeMethods.SendMessage(UnsafeNativeMethods.GetFocus(), NativeMethods.WM_KEYDOWN, _lastKeyDown.WParam, _lastKeyDown.LParam);
                            }
                        }
                        break;

                    case NativeMethods.WM_CHAR:

                        if ((Control.ModifierKeys & (Keys.Control | Keys.Alt)) != 0)
                        {
                            break;
                        }

                        if (PropertyGrid != null)
                        {
                            PropertyGrid.Focus();
                            UnsafeNativeMethods.SetFocus(new HandleRef(PropertyGrid, PropertyGrid.Handle));
                            Application.DoEvents();
                        }
                        else
                        {
                            break;
                        }

                        // Make sure we changed focus properly recreate the keystroke to the newly activated window
                        if (PropertyGrid.Focused || PropertyGrid.ContainsFocus)
                        {
                            IntPtr hWnd = UnsafeNativeMethods.GetFocus();
                            NativeMethods.SendMessage(hWnd, NativeMethods.WM_KEYDOWN, _lastKeyDown.WParam, _lastKeyDown.LParam);
                            NativeMethods.SendMessage(hWnd, NativeMethods.WM_CHAR, m.WParam, m.LParam);
                            return;
                        }
                        break;

                }
                base.WndProc(ref m);
            }

        }

        /// <devdoc>
        ///       The <see cref='System.ComponentModel.Design.CollectionEditor.CollectionForm'/> provides a modal dialog for editing the contents of a collection.
        /// </devdoc>
        [SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors")] //breaking change
        protected abstract class CollectionForm : Form
        {

            // Manipulation of the collection.
            //
            private readonly CollectionEditor _editor;
            private object _value;
            private short _editableState = EditableDynamic;

            private const short EditableDynamic = 0;
            private const short EditableYes = 1;
            private const short EditableNo = 2;
            
            /// <devdoc>
            ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.CollectionEditor.CollectionForm'/> class.
            /// </devdoc>
            public CollectionForm(CollectionEditor editor)
            {
                _editor = editor;
            }
            
            /// <devdoc>
            ///       Gets or sets the data type of each item in the collection.
            /// </devdoc>
            protected Type CollectionItemType
            {
                get
                {
                    return _editor.CollectionItemType;
                }
            }
            
            /// <devdoc>
            ///       Gets or sets the type of the collection.
            /// </devdoc>
            protected Type CollectionType
            {
                get
                {
                    return _editor.CollectionType;
                }
            }
            
            internal virtual bool CollectionEditable
            {
                get
                {
                    if (_editableState != EditableDynamic)
                    {
                        return _editableState == EditableYes;
                    }

                    bool editable = typeof(IList).IsAssignableFrom(_editor.CollectionType);

                    if (editable)
                    {
                        if (EditValue is IList list)
                        {
                            return !list.IsReadOnly;
                        }
                    }
                    return editable;
                }
                set
                {
                    if (value)
                    {
                        _editableState = EditableYes;
                    }
                    else
                    {
                        _editableState = EditableNo;
                    }
                }
            }
            
            /// <devdoc>
            ///       Gets or sets a type descriptor that indicates the current context.
            /// </devdoc>
            protected ITypeDescriptorContext Context
            {
                get
                {
                    return _editor.Context;
                }
            }
            
            /// <devdoc>
            ///    Gets or sets the value of the item being edited.
            /// </devdoc>
            public object EditValue
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    OnEditValueChanged();
                }
            }
            
            /// <devdoc>
            ///       Gets or sets the array of items this form is to display.
            /// </devdoc>
            protected object[] Items
            {
                get
                {
                    return _editor.GetItems(EditValue);
                }
                [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
                set
                {
                    // Request our desire to make a change.
                    //
                    bool canChange = false;
                    try
                    {
                        canChange = Context.OnComponentChanging();
                    }
                    catch (Exception ex)
                    {
                        if (!ClientUtils.IsCriticalException(ex))
                        {
                            DisplayError(ex);
                        }
                        else
                        {
                            throw;
                        }
                    }
                    if (canChange)
                    {
                        object newValue = _editor.SetItems(EditValue, value);
                        if (newValue != EditValue)
                        {
                            EditValue = newValue;
                        }
                        Context.OnComponentChanged();
                    }

                }
            }
            
            /// <devdoc>
            ///       Gets or sets the available item types that can be created for this collection.
            /// </devdoc>
            protected Type[] NewItemTypes
            {
                get
                {
                    return _editor.NewItemTypes;
                }
            }
            
            /// <devdoc>
            ///    Gets or sets a value indicating whether original members of the collection can be removed.
            /// </devdoc>
            protected bool CanRemoveInstance(object value)
            {
                return _editor.CanRemoveInstance(value);
            }
            
            /// <devdoc>
            ///    Gets or sets a value indicating whether multiple collection members can be selected.
            /// </devdoc>
            protected virtual bool CanSelectMultipleInstances()
            {
                return _editor.CanSelectMultipleInstances();
            }
            
            /// <devdoc>
            ///       Creates a new instance of the specified collection item type.
            /// </devdoc>
            protected object CreateInstance(Type itemType)
            {
                return _editor.CreateInstance(itemType);
            }
            
            /// <devdoc>
            ///       Destroys the specified instance of the object.
            /// </devdoc>
            protected void DestroyInstance(object instance)
            {
                _editor.DestroyInstance(instance);
            }
            
            /// <devdoc>
            ///    Displays the given exception to the user.
            /// </devdoc>
            protected virtual void DisplayError(Exception e)
            {
                IUIService uis = (IUIService)GetService(typeof(IUIService));
                if (uis != null)
                {
                    uis.ShowError(e);
                }
                else
                {
                    string message = e.Message;
                    if (message == null || message.Length == 0)
                    {
                        message = e.ToString();
                    }
                    RTLAwareMessageBox.Show(null, message, null, MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, 0);
                }
            }
            
            /// <devdoc>
            ///       Gets the requested service, if it is available.
            /// </devdoc>
            protected override object GetService(Type serviceType)
            {
                return _editor.GetService(serviceType);
            }
            
            /// <devdoc>
            ///       Called to show the dialog via the IWindowsFormsEditorService
            /// </devdoc>
            protected internal virtual DialogResult ShowEditorDialog(IWindowsFormsEditorService edSvc)
            {
                return edSvc.ShowDialog(this);
            }
            
            /// <devdoc>
            ///       This is called when the value property in the <see cref='System.ComponentModel.Design.CollectionEditor.CollectionForm'/> has changed.
            /// </devdoc>
            protected abstract void OnEditValueChanged();
        }


        internal class PropertyGridSite : ISite
        {

            private readonly IServiceProvider _sp;
            private readonly IComponent _comp;
            private bool _inGetService = false;

            public PropertyGridSite(IServiceProvider sp, IComponent comp)
            {
                _sp = sp;
                _comp = comp;
            }

            /** The component sited by this component site. */
            /// <devdoc>
            ///    When implemented by a class, gets the component associated with the <see cref='System.ComponentModel.ISite'/>.
            /// </devdoc>
            public IComponent Component { get { return _comp; } }

            /** The container in which the component is sited. */
            /// <devdoc>
            /// When implemented by a class, gets the container associated with the <see cref='System.ComponentModel.ISite'/>.
            /// </devdoc>
            public IContainer Container { get { return null; } }

            /** Indicates whether the component is in design mode. */
            /// <devdoc>
            ///    When implemented by a class, determines whether the component is in design mode.
            /// </devdoc>
            public bool DesignMode { get { return false; } }

            /** 
             * The name of the component.
             */
            /// <devdoc>
            ///    When implemented by a class, gets or sets the name of the component associated with the <see cref='System.ComponentModel.ISite'/>.
            /// </devdoc>
            public string Name
            {
                get { return null; }
                set { }
            }

            public object GetService(Type t)
            {
                if (!_inGetService && _sp != null)
                {
                    try
                    {
                        _inGetService = true;
                        return _sp.GetService(t);
                    }
                    finally
                    {
                        _inGetService = false;
                    }
                }
                return null;
            }

        }
    }
}


