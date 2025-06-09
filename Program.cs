using System;
using Gtk;
using Gdk;

using System.IO;
using System.Text;





class Program
{
    // widget initiation
    static TextView textView;
    static Toolbar toolbar;
    static ColorButton colorButton;
    static Entry searchEntry;

    static Label encoderView;
    //global variables
    static string currentFilePath;
    static bool toggling = false;
    static string lastSearchText = null;
    static Encoding currentEncoding = Encoding.UTF8; // default to UTF-8

    static Stack<string> undoStack = new Stack<string>(); //stack for the undo
    static Stack<string> redoStack = new Stack<string>(); //stack for the redo

    static bool stopHistory = false; //bool for the undo/redo functionality

    enum Tool { Bold, Italic }; // useless code to be implented if needed 





    static void Main(string[] args)
    {
        Application.Init();

        var window = new Gtk.Window("Text Editor");
        window.SetDefaultSize(800, 600);
        window.SetPosition(WindowPosition.Center);
        window.DeleteEvent += (o, e) => Application.Quit();

        VBox vbox = new VBox(false, 2);

        // Menu bar
        MenuBar menuBar = new MenuBar();

        // File Menu
        Menu fileMenu = new Menu();
        MenuItem file = new MenuItem("File");
        MenuItem open = new MenuItem("Open");
        MenuItem save = new MenuItem("Save");
        MenuItem quit = new MenuItem("Quit");



        open.Activated += OnOpen;
        save.Activated += OnSave;
        quit.Activated += (sender, e) => Application.Quit();

        fileMenu.Append(open);
        fileMenu.Append(save);
        fileMenu.Append(new SeparatorMenuItem());
        fileMenu.Append(quit);

        file.Submenu = fileMenu;

        menuBar.Append(file);

        // Edit Menu
        Menu editMenu = new Menu();
        MenuItem edit = new MenuItem("Edit");
        MenuItem undo = new MenuItem("Undo");
        MenuItem redo = new MenuItem("Redo");

        undo.Activated += OnUndo;
        redo.Activated += OnRedo;

        editMenu.Append(undo);
        editMenu.Append(redo);

        edit.Submenu = editMenu;
        menuBar.Append(edit);

        // Encoding Menu
        Menu EncodingMenu = new Menu();
        MenuItem encoding = new MenuItem("Encoding");
        MenuItem utf8 = new MenuItem("UTF-8");
        MenuItem utf16 = new MenuItem("UTF-16");
        MenuItem utf32 = new MenuItem("UTF-32");
        MenuItem ascii = new MenuItem("ASCII");

        utf8.Activated += OnUtf8;
        utf16.Activated += OnUtf16;
        utf32.Activated += OnUtf32;
        ascii.Activated += OnAscii;

        EncodingMenu.Append(utf8);
        EncodingMenu.Append(utf16);
        EncodingMenu.Append(utf32);
        EncodingMenu.Append(ascii);

        encoding.Submenu = EncodingMenu;
        menuBar.Append(encoding);

        vbox.PackStart(menuBar, false, false, 0);

        // Toolbar
        toolbar = new Toolbar();
        toolbar.Style = ToolbarStyle.Icons;

        //image variables
        // Image boldIcon = new Image("icon/bold.png");
        // Image italicIcon = new Image("icon/italic.png");

        // add buttons
        // toolbar.Add(toggleToolButton(boldIcon, Tool.Bold, "bold"));
        // toolbar.Add(toggleToolButton(italicIcon, Tool.Italic, "italic"));
        // toolbar.Add(new SeparatorToolItem());

        // add the color picker
        ColorButton colorButton = new ColorButton();
        colorButton.Rgba = new Gdk.RGBA { Red = 1, Green = 1, Blue = 1, Alpha = 0 };
        colorButton.UseAlpha = true;
        colorButton.ColorSet += OnColorSet;

        ToolItem colorItem = new ToolItem();
        colorItem.Add(colorButton);
        toolbar.Add(colorItem);

        vbox.PackStart(toolbar, false, false, 0);

        // horizontal seperator
        Separator topSeparator = new Separator(Orientation.Horizontal);
        vbox.PackStart(topSeparator, false, false, 0);

        // TextView
        // Create a horizontal box to hold the TextView and line counter for the 
        HBox hbox = new HBox();

        // Main Textview
        textView = new TextView { WrapMode = WrapMode.WordChar };
        ScrolledWindow textScroll = new ScrolledWindow { HscrollbarPolicy = PolicyType.Never, VscrollbarPolicy = PolicyType.Automatic };
        //textView.KeyPressEvent += OnTextViewKeyPress;
        textView.KeyPressEvent += (o, args) =>
        {
            

            // Ctrl + D for Deleteing a line
            if ((args.Event.State & ModifierType.ControlMask) != 0 && args.Event.Key == Gdk.Key.d)
            {
                DeleteCurrentLine();
                args.RetVal = true;
            }

            // Ctrl + Shift + C for Copying a line
            else if ((args.Event.State & ModifierType.ControlMask) != 0 &&
                    (args.Event.State & ModifierType.ShiftMask) != 0 &&
                    args.Event.Key == Gdk.Key.c)
            {
                CopyCurrentLine();
                args.RetVal = true;
            }
        };
        textScroll.Add(textView);

        // Line Counter TextView
        TextView lineCounter = new TextView { Editable = false, CursorVisible = false };
        lineCounter.ModifyBg(StateType.Normal, new Gdk.Color(240, 240, 240));
        lineCounter.WidthRequest = 18;
        ScrolledWindow counterScroll = new ScrolledWindow { HscrollbarPolicy = PolicyType.Never, VscrollbarPolicy = PolicyType.Never };
        counterScroll.Add(lineCounter);
        textScroll.Vadjustment.ValueChanged += (sender, e) => { counterScroll.Vadjustment.Value = textScroll.Vadjustment.Value; };

        // Text changed handler to update line numbers
        textView.Buffer.Changed += (sender, e) =>
        {
            int lineCount = textView.Buffer.LineCount;
            string lines = string.Join("\n", Enumerable.Range(1, lineCount));
            lineCounter.Buffer.Text = lines;
        };

        // Pack both into hbox
        hbox.PackStart(counterScroll, false, false, 0);
        hbox.PackStart(textScroll, true, true, 0);

        //  adding hbox to your main vbox
        vbox.PackStart(hbox, true, true, 0);

        // horizontal seperator
        Separator bottomSeparator = new Separator(Orientation.Horizontal);
        vbox.PackStart(bottomSeparator, false, false, 0);

        // statusbar
        Box statusBar = new Box(Orientation.Horizontal, 2);
        statusBar.PackStart(new Label("look up:"), false, false, 5);
        Entry searchEntry = new Entry();
        statusBar.PackStart(searchEntry, false, false, 3);

        // search button
        Button searchbtn = new Button("search");
        searchbtn.Clicked += (sender, e) =>
        {
            string searchText = searchEntry.Text;
            if (!string.IsNullOrEmpty(searchText))
                lastSearchText = searchText;
                SearchText(searchText);
        };
        statusBar.PackStart(searchbtn, false, false, 0);

        Button replaceBtn = new Button("Replace");
        replaceBtn.Clicked += (sender, e) =>
        {

            string replacewith = searchEntry.Text;
            if (!string.IsNullOrEmpty(lastSearchText) && !string.IsNullOrEmpty(replacewith))
                Replacetext(lastSearchText, replacewith);
        };
        statusBar.PackStart(replaceBtn, false, false, 0);
        statusBar.PackStart(new SeparatorToolItem(), false, false, 10);

        // line number counter label
        Label lineCountLabel = new Label("lines: 0");
        statusBar.PackStart(lineCountLabel, false, false, 0);
        textView.Buffer.Changed += (sender, e) =>
        {
            int lineCount = textView.Buffer.EndIter.Line + 1;
            // lineCountLabel.Text = $"lines: {lineCount - 1}";
            lineCountLabel.Text = $"lines: {lineCount}";

        };
        Alignment statusBarContainer = new Alignment(0, 1, 1, 0);
        statusBarContainer.SetPadding(2, 2, 2, 2);
        statusBarContainer.Add(statusBar);
        statusBar.PackStart(new SeparatorToolItem(), false, false, 10);
        // utf encoding view
        encoderView = new Label("UTF-16");
        statusBar.PackStart(encoderView, false, false, 0);

        // cursor position
        statusBar.PackStart(new SeparatorToolItem(), false, false, 10);
        Label cursorposition = new Label("Ln 1, Col 1");
        statusBar.PackStart(cursorposition, false, false, 0);
        textView.Buffer.MarkSet += (object o, Gtk.MarkSetArgs args) =>
        {
            Gtk.TextIter iter = textView.Buffer.GetIterAtMark(textView.Buffer.InsertMark);
            int line = iter.Line + 1;
            int col = iter.LineOffset + 1;
            cursorposition.Text = $"Ln {line}, Col {col}";
        };


        // Pack it to the main layout
        vbox.PackEnd(statusBarContainer, false, false, 0);




        window.Add(vbox);
        window.ShowAll();

        // Show the popup here
        MessageDialog infoDialog = new MessageDialog(window, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, 
            "Welcome!\n For line operations. \n Use CTRL+D for delete line. \n Use CTRL+Shift+C for Copy line operations."
        );
        infoDialog.Title = "Information";
        infoDialog.Run();
        infoDialog.Destroy();

        Application.Run();
    }

    static void OnOpen(object sender, EventArgs args)
    {
        FileChooserDialog fc = new FileChooserDialog("Open file", null,
            FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);

        // adding a text file filter
        FileFilter txtFilter = new FileFilter();
        txtFilter.Name = "Text files";
        txtFilter.AddPattern("*.txt");
        fc.AddFilter(txtFilter);

        if (fc.Run() == (int)ResponseType.Accept)
        {
            currentFilePath = fc.Filename;
            textView.Buffer.Text = File.ReadAllText(currentFilePath, currentEncoding);
            UpdateEncodingLabel();
        }

        fc.Destroy();
    }


    static void OnSave(object sender, EventArgs args)
    {
        if (string.IsNullOrEmpty(currentFilePath))
        {
            FileChooserDialog fc = new FileChooserDialog("Save file", null,
                FileChooserAction.Save, "Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);

            // adding a text file filter
            FileFilter txtFilter = new FileFilter();
            txtFilter.Name = "Text files";
            txtFilter.AddPattern("*.txt");
            fc.AddFilter(txtFilter);

            fc.DoOverwriteConfirmation = true;

            if (fc.Run() == (int)ResponseType.Accept)
            {
                currentFilePath = fc.Filename;
                if (!currentFilePath.EndsWith(".txt"))
                {
                    currentFilePath += ".txt"; // ensure extension
                }

            }

            fc.Destroy();
        }

        if (!string.IsNullOrEmpty(currentFilePath))
        {

            File.WriteAllText(currentFilePath, textView.Buffer.Text, currentEncoding);
        }
    }

    // the undo and redo functions
    static void OnTextChanged(object sender, EventArgs e)
    {
        if (stopHistory) return;

        string currentText = textView.Buffer.Text;
        if (undoStack.Count == 0 || undoStack.Peek() != currentText)
        {
            undoStack.Push(currentText);
            redoStack.Clear();
        }
    }

    static void OnUndo(object sender, EventArgs e)
    {
        if (undoStack.Count > 1)
        {
            stopHistory = true;

            string current = undoStack.Pop();
            redoStack.Push(current);

            string prev = undoStack.Peek();
            textView.Buffer.Text = prev;

            stopHistory = false;
        }
    }

    static void OnRedo(object sender, EventArgs e)
    {
        if (redoStack.Count > 0)
        {
            stopHistory = true;

            string redoText = redoStack.Pop();
            undoStack.Push(redoText);
            textView.Buffer.Text = redoText;

            stopHistory = false;
        }
    }

    static void OnUtf8(object sender, EventArgs e) { currentEncoding = Encoding.UTF8; UpdateEncodingLabel(); }
    static void OnUtf16(object sender, EventArgs e) { currentEncoding = Encoding.Unicode; UpdateEncodingLabel(); }

    static void OnUtf32(object sender, EventArgs e) { currentEncoding = Encoding.UTF32; UpdateEncodingLabel(); }

    static void OnAscii(object sender, EventArgs e) { currentEncoding = Encoding.ASCII; UpdateEncodingLabel(); }

    // static ToggleToolButton toggleToolButton(Image icon, Tool tool, string tooltip)
    // {
    //     ToggleToolButton button = new ToggleToolButton
    //     {
    //         IconWidget = icon,
    //         TooltipText = tooltip
    //     };

    //     button.Clicked += (sender, e) => Toggle(sender, tool);
    //     button.ShowAll();
    //     return button;
    // }

    static void Toggle(object sender, Tool tool)
    {
        if (toggling)
            return;

        toggling = true;

        foreach (ToolItem item in toolbar)
        {
            if (item is ToggleToolButton ttb)
            {
                if (item != sender)
                    ttb.Active = false;
            }
        }
        // Apply effect: demo print
        Console.WriteLine($"Selected tool: {tool}");

        toggling = false;
    }

    static void OnColorSet(object sender, EventArgs e)
    {
        ColorButton cb = (ColorButton)sender;
        Gdk.RGBA selectedColor = cb.Rgba;

        // Access buffer
        TextBuffer buffer = textView.Buffer;
        if (!buffer.GetSelectionBounds(out TextIter start, out TextIter end))
            return; // Nothing selected

        // Create a unique tag name based on color
        string tagName = $"highlight_{selectedColor.ToString().Replace(" ", "")}";

        // Try to find an existing tag, or create one
        TextTag tag = buffer.TagTable.Lookup(tagName);
        if (tag == null)
        {
            tag = new TextTag(tagName);
            tag.BackgroundRgba = selectedColor;
            buffer.TagTable.Add(tag);
        }

        // Apply the tag to the selected range
        buffer.ApplyTag(tag, start, end);
    }

    static void SearchText(string searchText)
    {
        TextBuffer buffer = textView.Buffer;

        // Remove previous tags
        TextTagTable tagTable = buffer.TagTable;
        TextTag highlightTag = tagTable.Lookup("highlight") ?? new TextTag("highlight");
        highlightTag.Background = "light blue";
        if (!tagTable.Lookup("highlight")?.Equals(highlightTag) ?? true)
            tagTable.Add(highlightTag);

        // Clear old highlights
        TextIter start, end;
        buffer.GetBounds(out start, out end);
        buffer.RemoveTag(highlightTag, start, end);

        // Search and highlight
        TextIter matchStart = buffer.StartIter;
        while (matchStart.ForwardSearch(searchText, TextSearchFlags.CaseInsensitive, out TextIter matchBegin, out TextIter matchEnd, buffer.EndIter))
        {
            buffer.ApplyTag(highlightTag, matchBegin, matchEnd);
            matchStart = matchEnd; // Move past the last match
        }
    }

    static void Replacetext(string searchText, string replaceText)
    {
        TextBuffer buffer = textView.Buffer;
        buffer.GetBounds(out TextIter start, out TextIter end);

        string fullText = buffer.GetText(start, end, false);
        string updatedText = System.Text.RegularExpressions.Regex.Replace(
            fullText,
            System.Text.RegularExpressions.Regex.Escape(searchText),
            replaceText,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        buffer.Text = updatedText;
    }



    // tge delete line function
    static void DeleteCurrentLine()
    {
        TextIter cursorIter = textView.Buffer.GetIterAtMark(textView.Buffer.InsertMark);
        int lineNumber = cursorIter.Line;

        TextIter lineStart = textView.Buffer.GetIterAtLineOffset(lineNumber, 0);
        TextIter lineEnd = lineStart;
        lineEnd.ForwardToLineEnd();

        // Try to include newline character if not the last line
        TextIter maybeNewline = lineEnd;
        if (maybeNewline.ForwardChar())
        {
            lineEnd = maybeNewline;
        }

        textView.Buffer.Delete(ref lineStart, ref lineEnd);
    }


    // the copy line delete function
    static void CopyCurrentLine()
    {
        TextIter cursorIter = textView.Buffer.GetIterAtMark(textView.Buffer.InsertMark);
        int lineNumber = cursorIter.Line;

        TextIter lineStart = textView.Buffer.GetIterAtLineOffset(lineNumber, 0);
        TextIter lineEnd = lineStart;
        lineEnd.ForwardToLineEnd();

        string lineText = textView.Buffer.GetText(lineStart, lineEnd, false);

        Clipboard clipboard = Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
        clipboard.Text = lineText;
    }





    static void UpdateEncodingLabel()
    {
        if (currentEncoding == Encoding.UTF8)
            encoderView.Text = "UTF-8";
        else if (currentEncoding == Encoding.Unicode)  // UTF-16
            encoderView.Text = "UTF-16";
        else if (currentEncoding == Encoding.UTF32)
            encoderView.Text = "UTF-32";
        else if (currentEncoding == Encoding.ASCII)
            encoderView.Text = "ASCII";
        else
            encoderView.Text = "Unknown";
    }




}


