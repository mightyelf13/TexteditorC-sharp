using System;
using Gtk;
using Gdk;

using System.IO;
using System.Text;


namespace TextEditorApp
{
    class Program
    {
        private TextView? textView;
        private Label? encoderView;

        // state
        private string? currentFilePath;
        private string? lastSearchText = null;

        private Encoding currentEncoding = Encoding.UTF8; // default to UTF-8

        public static void Main(string[] args)
        {
            Application.Init();
            new Program().APp(); // create one instance and let it build the UI
            Application.Run();  // hand control to GTK main-loop
        }


        public void APp()
        {
            var window = new Gtk.Window("Text Editor")
            {
                DefaultWidth = 800,
                DefaultHeight = 600,
                WindowPosition = WindowPosition.Center
            };
            window.DeleteEvent += (_, _) => Application.Quit();

            var vbox = new Box(Orientation.Vertical, 2);
            vbox.PackStart(CreateMenuBar(), false, false, 0);
            // var toolbar = CreateToolbar();
            vbox.PackStart(CreateToolbar(), false, false, 0);
            vbox.PackStart(new Separator(Orientation.Horizontal), false, false, 0);
            vbox.PackStart(CreateEditorArea(), true, true, 0);
            vbox.PackStart(new Separator(Orientation.Horizontal), false, false, 0);
            vbox.PackEnd(CreateStatusBar(), false, false, 0);

            window.Add(vbox);
            window.ShowAll();

            // Show the popup here
            using var info = new MessageDialog(window, DialogFlags.Modal,
                                               MessageType.Info, ButtonsType.Ok,
                "Welcome!\nLine operations:\n  • Ctrl+D – delete line\n  • Ctrl+Shift+C – copy line");
            info.Title = "Information";
            info.Run();

            UpdateEncodingLabel();
        }

        

        public MenuBar CreateMenuBar()  
        {
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

            return menuBar;
        }

        public Toolbar CreateToolbar()
        {
            var toolbar = new Toolbar
            {
                Style = ToolbarStyle.Icons
            };

            // add the color picker
            ColorButton colorButton = new ColorButton
            {
                Rgba = new Gdk.RGBA { Red = 1, Green = 1, Blue = 1, Alpha = 0 },
                UseAlpha = true
            };
            colorButton.ColorSet += OnColorSet;

            ToolItem colorItem = new ToolItem();
            colorItem.Add(colorButton);
            toolbar.Add(colorItem);

            return toolbar;
        }

        public Box CreateEditorArea()
        {
            Box hbox = new Box(Orientation.Horizontal, 6);


            // Main TextView
            textView = new TextView { WrapMode = WrapMode.WordChar };
            ScrolledWindow textScroll = new ScrolledWindow { HscrollbarPolicy = PolicyType.Automatic, VscrollbarPolicy = PolicyType.Automatic };

            textView.KeyPressEvent += (o, args) =>
            {
                // Ctrl + D: Delete line
                if ((args.Event.State & ModifierType.ControlMask) != 0 && args.Event.Key == Gdk.Key.d)
                {
                    DeleteCurrentLine();
                    args.RetVal = true;
                }

                // Ctrl + Shift + C: Copy line
                else if ((args.Event.State & ModifierType.ControlMask) != 0 &&
                        (args.Event.State & ModifierType.ShiftMask) != 0 &&
                        args.Event.Key == Gdk.Key.c)
                {
                    CopyCurrentLine();
                    args.RetVal = true;
                }
            };

            textScroll.Add(textView);

            // Line Counter
            TextView lineCounter = new TextView
            {
                Editable = false,
                CursorVisible = false,
                Sensitive = false,

            };
            lineCounter.ModifyBg(StateType.Normal, new Gdk.Color(240, 240, 240)); // couldnt find a newer method and this works fine 

            lineCounter.WidthRequest = 18;

            ScrolledWindow counterScroll = new ScrolledWindow
            {
                HscrollbarPolicy = PolicyType.Automatic,
                VscrollbarPolicy = PolicyType.Automatic,   
            };

            



            //Sync scrolling
            textScroll.Vadjustment.ValueChanged += (sender, e) =>
            {
                counterScroll.Vadjustment.Value = textScroll.Vadjustment.Value;
            };
            counterScroll.Add(lineCounter);
            // Update line numbers on text change
            int previousLineCount = 0;

            textView.Buffer.Changed += (sender, e) =>
            {
                int currentLineCount = textView.Buffer.LineCount;

                // Update only if line count changes
                if (currentLineCount != previousLineCount)
                {
                    string lines = string.Join("\n", Enumerable.Range(1, currentLineCount));
                    lineCounter.Buffer.Text = lines;
                    previousLineCount = currentLineCount;
                }
            };


            // Pack both views into hbox
            hbox.PackStart(counterScroll, false, false, 0);
            hbox.PackStart(textScroll, true, true, 0);

            return hbox;
        }

        // this is to create the status bar with search, line count, encoding and cursor position

        public Box CreateStatusBar()
        {
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
                {
                    lastSearchText = searchText;
                    SearchText(searchText);
                }
            };
            statusBar.PackStart(searchbtn, false, false, 0);

            // replace button
            Button replaceBtn = new Button("Replace");
            replaceBtn.Clicked += (sender, e) =>
            {
                string replacewith = searchEntry.Text;
                if (!string.IsNullOrEmpty(lastSearchText) && !string.IsNullOrEmpty(replacewith))
                    Replacetext(lastSearchText, replacewith);
            };
            statusBar.PackStart(replaceBtn, false, false, 0);
            statusBar.PackStart(new SeparatorToolItem(), false, false, 10);

            // line counter
            Label lineCountLabel = new Label("lines: 0");
            statusBar.PackStart(lineCountLabel, false, false, 0);
            textView!.Buffer.Changed += (sender, e) =>
            {
                int lineCount = textView.Buffer.EndIter.Line + 1;
                lineCountLabel.Text = $"lines: {lineCount}";
            };

            // utf encoding view
            encoderView = new Label("UTF-16");
            statusBar.PackStart(new SeparatorToolItem(), false, false, 10);
            statusBar.PackStart(encoderView, false, false, 0);

            // cursor position label
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

            Box statusBarContainer = new Box(Orientation.Vertical, 0);
            statusBar.Margin = 2; // Uniform padding
            statusBarContainer.PackEnd(statusBar, false, false, 0);


            return statusBarContainer;
        }


        public void OnOpen(object? sender, EventArgs args)
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

                try
                {
                    textView!.Buffer.Text = File.ReadAllText(currentFilePath, currentEncoding);
                    UpdateEncodingLabel();
                }
                catch (UnauthorizedAccessException)
                {
                    ShowErrorDialog("Access denied", $"You don't have permission to open '{currentFilePath}'.");
                }
                catch (Exception ex)
                {
                    ShowErrorDialog("Error opening file", ex.Message);
                }
            }

            fc.Destroy();
        }

        private void ShowErrorDialog(string title, string message)
        {
            MessageDialog md = new MessageDialog(null,
                DialogFlags.Modal, MessageType.Error, ButtonsType.Ok, message);
            md.Title = title;
            md.Run();
            md.Destroy();
        }



        public void OnSave(object? sender, EventArgs args)
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

                File.WriteAllText(currentFilePath, textView!.Buffer.Text, currentEncoding);
            }
        }

        public void OnUtf8(object? sender, EventArgs e) { currentEncoding = Encoding.UTF8; UpdateEncodingLabel(); }
        public void OnUtf16(object? sender, EventArgs e) { currentEncoding = Encoding.Unicode; UpdateEncodingLabel(); }

        public void OnUtf32(object? sender, EventArgs e) { currentEncoding = Encoding.UTF32; UpdateEncodingLabel(); }

        public void OnAscii(object? sender, EventArgs e) { currentEncoding = Encoding.ASCII; UpdateEncodingLabel(); }

        public void OnColorSet(object? sender, EventArgs e)
        {
            ColorButton cb = (ColorButton)sender!;
            Gdk.RGBA selectedColor = cb.Rgba;

            // Access buffer
            TextBuffer buffer = textView!.Buffer;
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

        public void SearchText(string searchText)
        {
            TextBuffer buffer = textView!.Buffer;

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

        public void Replacetext(string searchText, string replaceText)
        {
            TextBuffer buffer = textView!.Buffer;
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



        // the delete line function
        public void DeleteCurrentLine()
        {
            TextIter cursorIter = textView!.Buffer.GetIterAtMark(textView.Buffer.InsertMark);
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
        public void CopyCurrentLine()
        {
            TextIter cursorIter = textView!.Buffer.GetIterAtMark(textView.Buffer.InsertMark);
            int lineNumber = cursorIter.Line;

            TextIter lineStart = textView.Buffer.GetIterAtLineOffset(lineNumber, 0);
            TextIter lineEnd = lineStart;
            lineEnd.ForwardToLineEnd();

            string lineText = textView.Buffer.GetText(lineStart, lineEnd, false);

            Clipboard clipboard = Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
            clipboard.Text = lineText;
        }

        public void UpdateEncodingLabel()
        {
            if (currentEncoding == Encoding.UTF8)
                encoderView!.Text = "UTF-8";
            else if (currentEncoding == Encoding.Unicode)  // UTF-16
                encoderView!.Text = "UTF-16";
            else if (currentEncoding == Encoding.UTF32)
                encoderView!.Text = "UTF-32";
            else if (currentEncoding == Encoding.ASCII)
                encoderView!.Text = "ASCII";
            else
                encoderView!.Text = "Unknown";
        }

    }
}
