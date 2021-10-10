// Copyright (c) 2011 Aaron Moss
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace LiveWiki
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //flag for whether this has live preview turned on
        bool IsLivePreview = true;
		//integer user ID for testing
		string UserId = "0";
		//log output file
		StreamWriter LogFile = null;
		//time logfile was opened
		DateTime LogStart = DateTime.Now;

		//flag for programattic selection modification
		bool isProgSelection = false;
		//bookkeeping to check when the currently selected paragraph changes
		Paragraph insertPara, insertEndPara;


		/* SelectAdorner selectAdorner; */

        public MainWindow()
        {
			SetupDialog setupDlg = new SetupDialog(
				(bool isLivePreview, string userId) => { IsLivePreview = isLivePreview; UserId = userId; }
				);
			bool? didSetup = setupDlg.ShowDialog();
			if (didSetup != true) Close();

			Title = "LiveWiki " + (IsLivePreview ? "+ " : "- ") + UserId; 
			LogFile = new StreamWriter("ID" + UserId + "_LogFile.txt");
			LogFile.AutoFlush = true;
			LogStart = DateTime.Now;
			log("Start UID|" + UserId + "| LivePreview|" + IsLivePreview + "| {START:" + UserId + "}");
						
			// attach CommandBinding to root window
            this.CommandBindings.Add(new CommandBinding(
                WikiCommands.WikiBold, ExecutedWikiCommand, CanExecuteWikiCommand));
            this.CommandBindings.Add(new CommandBinding(
                WikiCommands.WikiItalic, ExecutedWikiCommand, CanExecuteWikiCommand));
            this.CommandBindings.Add(new CommandBinding(
                WikiCommands.WikiHeading, ExecutedWikiCommand, CanExecuteWikiCommand));

            InitializeComponent();

			//ensure document normalized initially
			Run initRun = emptyRun();
			editField.Document = new FlowDocument(new Paragraph(initRun));
			select(initRun.ContentEnd, initRun.ContentEnd);
			insertPara = editField.Selection.Start.Paragraph;
			insertEndPara = editField.Selection.End.Paragraph;
			insertPara.BorderBrush = Brushes.Navy;
			insertPara.BorderThickness = new Thickness(1.0);

			/*
			//add select adorner
			selectAdorner = new SelectAdorner(editField, insertPara, insertEndPara);
			AdornerLayer parentAdorner = AdornerLayer.GetAdornerLayer(editField);
			parentAdorner.Add(selectAdorner);
			*/
			 
			//focus editField
			editField.Focus();
        }

        public void RefreshPreview(object sender, RoutedEventArgs e)
        {
			log("show preview {SHOW}");
			//ensure edited paragraph is up-to-date
			formattedPara(insertPara, IGNORE_ALTERNATE);
			generatePreview();
			//generateStructurePreview();
        }

		private void generatePreview()
		{
			previewField.Document.Blocks.Clear();
			
			previewField.BeginChange();

			foreach (Block block in editField.Document.Blocks)
			{
				if (block is Paragraph)
				{
					Paragraph p = new Paragraph();
					
					//get formatted original paragraph
					Paragraph orig = formattedPara((Paragraph)block);
										
					foreach (Inline inline in orig.Inlines)
					{
						Run run = inline as Run;
						if (run == null) continue;

						FormatTag tag = getTag(run);
						if (tag.MarkupType == FormatType.Default)
						{
							//text run
							Run r = filledRun(run.Text, tag.Format, FormatType.Default);
							FormatTag.Apply(tag, r);
							p.Inlines.Add(r);
						}
					}

					previewField.Document.Blocks.Add(p);
				}
			}
			
			previewField.EndChange();
		}
		
        private void generateStructurePreview()
        {
            previewField.Document.Blocks.Clear();

            previewField.BeginChange();

            previewField.Document.Blocks.Add(new Paragraph(new Run("<Document>")));
            foreach (Block block in editField.Document.Blocks)
            {
                Paragraph p;
                if (block is Paragraph)
                {
					Paragraph pBlock = (Paragraph)block;
					ParagraphTag pTag = getTag(pBlock);

					string ps = "<Paragraph";
					if (insertPara == block) ps += " ISTART";
					if (insertEndPara == block) ps += " IEND";
					ps += (pTag.IsPlainText) ? " PLAIN" : " FORMATTED";
					ps += (pTag.Alternate == null) ? " ALT:NULL" : " ALT:" + pTag.Alternate.GetHashCode();
					ps += ">";
					p = new Paragraph(new Run(ps));

                    foreach (Inline inline in (pBlock.Inlines))
                    {
                        if (inline is Run)
                        {
                            Run run = (Run)inline;
                            string s = "<Run ";
                            if (run.Tag == null) s += "tag=\"null\" ";
                            else if (!(run.Tag is FormatTag)) s += "tagClass=\"" + run.Tag.GetType() + "\" ";
                            else
                            {
                                FormatTag tag = (FormatTag)run.Tag;
                                s += "format=\"" + tag.Format.Formats + "\" ";
                                s += "markup=\"" + tag.MarkupType + "\" ";
                            }
							s += ">" + run.Text + "</Run>";
                            p.Inlines.Add(new Run(s));
                        }
                        else
                        {
                            p.Inlines.Add(new Run("<Inline class=\"" + inline.GetType() + "\"/>"));
                        }
                    }
                    p.Inlines.Add(new Run("</Paragraph>"));
                }
                else
                {
                    p = new Paragraph(new Run("<Block class=\"" + block.GetType() + "\"/>"));
                }
                previewField.Document.Blocks.Add(p);
            }
            previewField.Document.Blocks.Add(new Paragraph(new Run("</Document>")));

            previewField.EndChange();
        }
		

        // CanExecuteRoutedEventHandler that always returns true.
        public void CanExecuteWikiCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        //execution handler for wiki command
        public void ExecutedWikiCommand(object sender, ExecutedRoutedEventArgs e)
        {
            WikiCommand cmd = (WikiCommand)e.Command;

            //MessageBox.Show("Fired " + cmd.FormatType + " command");
			TextRange selection = editField.Selection;
			log("fired command on selection |" + selection.Text + "| {CMD:" + cmd.FormatType + "}");
			if (!editField.Selection.IsEmpty)
			{
				selection.Text = cmd.Markup + selection.Text + cmd.Markup;
				select(selection.End, selection.End);
			}
			else
			{
				selection.Text = cmd.Markup + cmd.Hint + cmd.Markup;
				select(selection.Start.GetPositionAtOffset(cmd.Markup.Length), 
					selection.End.GetPositionAtOffset(-1 * cmd.Markup.Length));
			}
        }

		//execution handler for window close
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			//finish the log file
			log("Finish {FINISH:" + UserId + "}");
			LogFile.Close();

			StreamWriter outputWriter = new StreamWriter("ID" + UserId + "_Content.txt");
			foreach (Block block in editField.Document.Blocks)
			{
				outputWriter.WriteLine(new TextRange(block.ContentStart, block.ContentEnd).Text);
				outputWriter.WriteLine();
			}
			outputWriter.Close();
		}

		private void selectChanged(object sender, RoutedEventArgs e)
		{
			//ignore programattic selections
			if (isProgSelection)
			{
				isProgSelection = false;
				return;
			}

			Paragraph newInsertPara = editField.Selection.Start.Paragraph;
			Paragraph newInsertEndPara = editField.Selection.End.Paragraph;
			bool isForward = 
				editField.Selection.Start.GetOffsetToPosition(editField.Selection.End) >= 0;

			/*
			//draw selection box
			if (newInsertPara == newInsertEndPara || isForward)
				//single paragraph selection or forward multi-paragraph selection
				selectAdorner.update(newInsertPara, newInsertEndPara);
			else
				//backward multi paragraph selection
				selectAdorner.update(newInsertEndPara, newInsertPara);
			*/
			 
			//ignore selections which do not go out of paragraph
			if (insertPara == newInsertPara && insertEndPara == newInsertEndPara) return;

			editField.BeginChange();

			//paragraphs to ensure are formatted
			Paragraph prevToFormat = null, nextToFormat = null;
			//pointers to update selection to (in updated paragraphs)
			TextPointer newInsert = editField.Selection.Start;
			TextPointer newInsertEnd = editField.Selection.End;

			if (newInsertPara == newInsertEndPara)
			{
				//one paragraph contains both start and end points
				if (newInsertPara != null)
				{
					prevToFormat = newInsertPara.PreviousBlock as Paragraph;
					nextToFormat = newInsertPara.NextBlock as Paragraph;

					Paragraph plain = plainPara(newInsertPara);
					if (plain != newInsertPara)
					{
						//new insertion point is in formatted paragraph, fix

						//get offsets to pointers to save
						int insertOff = trueOffset(newInsertPara, editField.Selection.Start);
						int insertEndOff = trueOffset(newInsertPara, editField.Selection.End);

						//switch plain paragraph for formatted
						newInsertPara.SiblingBlocks.InsertAfter(newInsertPara, plain);
						newInsertPara.SiblingBlocks.Remove(newInsertPara);
						newInsertPara = plain;
						newInsertEndPara = plain;

						//get equivalent pointers into new paragraph
						newInsert = newInsertPara.ContentStart.GetPositionAtOffset(insertOff);
						newInsertEnd = newInsertEndPara.ContentStart.GetPositionAtOffset(insertEndOff);
					}

					//border paragraph
					plain.BorderBrush = Brushes.Navy;
					plain.BorderThickness = new Thickness(1.0);
				}
			}
			else
			{
				//different paragraphs contain new start and end points

				if (isForward)
				{
					//forward selection spanning paragraph break
					prevToFormat = (newInsertPara == null) ? null : newInsertPara.PreviousBlock as Paragraph;
					nextToFormat = (newInsertEndPara == null) ? null : newInsertEndPara.NextBlock as Paragraph;
										
					//unformat insertion paragraph
					Paragraph plain = null;
					if (newInsertPara != null)
					{
						plain = plainPara(newInsertPara);
						if (plain != newInsertPara)
						{
							//new insertion point is inside formatted paragraph, fix

							//get offset to pointer to save
							int insertOff = trueOffset(newInsertPara, editField.Selection.Start);

							//switch plain paragraph for formatted
							newInsertPara.SiblingBlocks.InsertAfter(newInsertPara, plain);
							newInsertPara.SiblingBlocks.Remove(newInsertPara);
							newInsertPara = plain;

							//get equivalent pointer into new paragraph
							newInsert = newInsertPara.ContentStart.GetPositionAtOffset(insertOff);
						}
					}

					//border paragraph
					plain.BorderBrush = Brushes.Navy;
					plain.BorderThickness = new Thickness(1.0);

					//unformat any subsequent formatted blocks
					Paragraph crnt = (plain == null) ? 
						editField.Document.Blocks.FirstBlock as Paragraph : plain.NextBlock as Paragraph;
					while (crnt != null && crnt != newInsertEndPara)
					{
						plain = plainPara(crnt);
						if (plain != crnt)
						{
							//current paragraph is formatted, fix
							crnt.SiblingBlocks.InsertAfter(crnt, plain);
							crnt.SiblingBlocks.Remove(crnt);
						}

						//border paragraph
						plain.BorderBrush = Brushes.Navy;
						plain.BorderThickness = new Thickness(1.0);

						crnt = plain.NextBlock as Paragraph;
					}

					//unformat insertion end paragraph
					if (crnt != null && crnt == newInsertEndPara)
					{
						plain = plainPara(newInsertEndPara);
						if (plain != newInsertEndPara)
						{
							//new insertion end point is inside formatted paragraph, fix

							//get offset to pointer to save
							int insertOff = trueOffset(newInsertEndPara, editField.Selection.End);

							//switch plain paragraph for formatted
							newInsertEndPara.SiblingBlocks.InsertAfter(newInsertEndPara, plain);
							newInsertEndPara.SiblingBlocks.Remove(newInsertEndPara);
							newInsertEndPara = plain;

							//get equivalent pointer into new paragraph
							newInsertEnd = newInsertEndPara.ContentStart.GetPositionAtOffset(insertOff);
						}

						//border paragraph
						plain.BorderBrush = Brushes.Navy;
						plain.BorderThickness = new Thickness(1.0);
					}
				}
				else
				{
					//backward selection spanning paragraph break
					prevToFormat = (newInsertEndPara == null) ? null : newInsertEndPara.PreviousBlock as Paragraph;
					nextToFormat = (newInsertPara == null) ? null : newInsertPara.NextBlock as Paragraph;

					//unformat insertion paragraph
					Paragraph plain = null;
					if (newInsertPara != null)
					{
						plain = plainPara(newInsertPara);
						if (plain != newInsertPara)
						{
							//new insertion point is inside formatted paragraph, fix

							//get offset to pointer to save
							int insertOff = trueOffset(newInsertPara, editField.Selection.Start);

							//switch plain paragraph for formatted
							newInsertPara.SiblingBlocks.InsertAfter(newInsertPara, plain);
							newInsertPara.SiblingBlocks.Remove(newInsertPara);
							newInsertPara = plain;

							//get equivalent pointer into new paragraph
							newInsert = newInsertPara.ContentStart.GetPositionAtOffset(insertOff);
						}
					}

					//border paragraph
					plain.BorderBrush = Brushes.Navy;
					plain.BorderThickness = new Thickness(1.0);

					//unformat any previous formatted blocks
					Paragraph crnt = (plain == null) ? 
						editField.Document.Blocks.LastBlock as Paragraph : plain.PreviousBlock as Paragraph;
					while (crnt != null && crnt != newInsertEndPara)
					{
						plain = plainPara(crnt);
						if (plain != crnt)
						{
							//current paragraph is formatted, fix
							crnt.SiblingBlocks.InsertAfter(crnt, plain);
							crnt.SiblingBlocks.Remove(crnt);
						}

						//border paragraph
						plain.BorderBrush = Brushes.Navy;
						plain.BorderThickness = new Thickness(1.0);

						crnt = plain.PreviousBlock as Paragraph;
					}

					//unformat insertion end paragraph
					if (crnt != null && crnt == newInsertEndPara)
					{
						plain = plainPara(newInsertEndPara);
						if (plain != newInsertEndPara)
						{
							//new insertion end point is inside formatted paragraph, fix

							//get offset to pointer to save
							int insertOff = trueOffset(newInsertEndPara, editField.Selection.End);

							//switch plain paragraph for formatted
							newInsertEndPara.SiblingBlocks.InsertAfter(newInsertEndPara, plain);
							newInsertEndPara.SiblingBlocks.Remove(newInsertEndPara);
							newInsertEndPara = plain;

							//get equivalent pointer into new paragraph
							newInsertEnd = newInsertEndPara.ContentStart.GetPositionAtOffset(insertOff);
						}

						//border paragraph
						plain.BorderBrush = Brushes.Navy;
						plain.BorderThickness = new Thickness(1.0);
					}
				}
			}

			//format paragraphs before and after selection region
			while (prevToFormat != null)
			{
				Paragraph formatted = formattedPara(prevToFormat, IGNORE_ALTERNATE);

				if (formatted != prevToFormat)
				{
					//current paragraph is plaintext, fix
					prevToFormat.SiblingBlocks.InsertAfter(prevToFormat, formatted);
					prevToFormat.SiblingBlocks.Remove(prevToFormat);
				}

				//unborder paragraph
				formatted.BorderBrush = null;
				formatted.BorderThickness = new Thickness(0.0);

				prevToFormat = formatted.PreviousBlock as Paragraph;
			}
			while (nextToFormat != null)
			{
				Paragraph formatted = formattedPara(nextToFormat, IGNORE_ALTERNATE);

				if (formatted != nextToFormat)
				{
					//current paragraph is plaintext, fix
					nextToFormat.SiblingBlocks.InsertAfter(nextToFormat, formatted);
					nextToFormat.SiblingBlocks.Remove(nextToFormat);
				}

				//unborder paragraph
				formatted.BorderBrush = null;
				formatted.BorderThickness = new Thickness(0.0);

				nextToFormat = formatted.NextBlock as Paragraph;
			}

			//update bookkeeping
			insertPara = newInsertPara;
			insertEndPara = newInsertEndPara;

			editField.EndChange();

			//fix selection after paragraph switching
			select(newInsert, newInsertEnd);

			return;
		}

		Paragraph formatPara(Paragraph orig)
		{
			String text = textBetween(orig.ContentStart, orig.ContentEnd);
			Paragraph p = new Paragraph();

			Format fmt = Format.Default;

			while (true) 
			{
				int markupStart = text.IndexOf('\'');
				int markupLen = 0;
				FormatType markupType = FormatType.Default;

				while (markupStart >= 0 && markupStart + 1 < text.Length && markupType == FormatType.Default)
				{
					if (text[markupStart + 1] == '\'')
					{
						//have italic, check for bold
						if (markupStart + 2 < text.Length && text[markupStart + 2] == '\'')
						{
							//have bold
							markupLen = 3;
							markupType = FormatType.Bold;
						}
						else
						{
							//just italic
							markupLen = 2;
							markupType = FormatType.Italic;
						}
					}
					else
					{
						//just a quote, find next candidate
						markupStart = text.IndexOf('\'', markupStart + 1);
					}
				}

				if (markupType == FormatType.Default) break; //break on no further markup candidates

				//add text before markup to paragraph
				String trimText = text.Substring(0, markupStart);
				Run trimRun = filledRun(trimText, fmt, FormatType.Default);
				p.Inlines.Add(trimRun);
				if (IsLivePreview) FormatTag.Apply(getTag(trimRun), trimRun);

				//trim text from paragraph
				text = text.Substring(markupStart);

				//get new format
				fmt = Format.Toggle(fmt, markupType);

				//add markup to paragraph
				trimText = text.Substring(0, markupLen);
				trimRun = filledRun(trimText, fmt, markupType);
				p.Inlines.Add(trimRun);
				if (IsLivePreview) FormatTag.Apply(getTag(trimRun), trimRun);

				//trim markup from paragraph
				text = text.Substring(markupLen);
			}

			//add remaining text to paragraph
			Run remRun = filledRun(text, fmt, FormatType.Default);
			p.Inlines.Add(remRun);
			if (IsLivePreview) FormatTag.Apply(getTag(remRun), remRun);

			return p;
		}

		/// <summary>
		/// Gets the true offset (characters only) of a pointer from the start of a paragraph
		/// </summary>
		/// <param name="para">The paragraph to look in</param>
		/// <param name="pointer">
		/// The pointer to get the offset for (must be inside the given paragraph - behaviour 
		/// undefined otherwise)
		/// </param>
		/// <returns>The number of characters between the start of the paragraph and this pointer</returns>
		int trueOffset(Paragraph para, TextPointer pointer)
		{
			int offset = 0;
			int toPointer, len;

			Inline crnt = para.Inlines.FirstInline;
			while (crnt != null)
			{
				toPointer = crnt.ContentStart.GetOffsetToPosition(pointer);
				len = crnt.ContentStart.GetOffsetToPosition(crnt.ContentEnd);

				if (toPointer < len)
				{
					offset += toPointer;
					crnt = null;
				}
				else
				{
					offset += len;
					crnt = crnt.NextInline;
				}
			}

			return offset;
		}

		/// <summary>
		/// Selects the text between two TextPointers
		/// </summary>
		/// <param name="start">The start pointer of the selection</param>
		/// <param name="end">The end pointer of the selection</param>
		void select(TextPointer start, TextPointer end)
		{
			//if (start == null || end == null || start.Parent == null || end.Parent == null) return;
			
			isProgSelection = true;
			editField.Selection.Select(start, end);
		}

		/// <summary>
		/// Gets the text between two text pointers
		/// </summary>
		/// <param name="start">The start pointer of the text range</param>
		/// <param name="end">The end pointer of the text range</param>
		/// <returns>The text between the two pointers</returns>
		string textBetween(TextPointer start, TextPointer end)
		{
			return new TextRange(start, end).Text;
		}

        /// <summary>
        /// Gets the formatting tag for a run, creating a default if needed
        /// </summary>
        /// <param name="run">The run to get the tag of</param>
        /// <returns>The formatting tag for the run (created if needed)</returns>
        FormatTag getTag(Run run)
        {
            if (run.Tag == null)
            {
                run.Tag = new FormatTag(Format.Default, FormatType.Default);
            }
            return (FormatTag)run.Tag;
        }

		/// <summary>
		/// Gets the formatting tag for a paragraph, creating a default if needed
		/// </summary>
		/// <param name="para">The paragraph to get the tag of</param>
		/// <returns>The formatting tag for the paragraph (created if needed)</returns>
		ParagraphTag getTag(Paragraph para)
		{
			if (para.Tag == null)
			{
				para.Tag = new ParagraphTag();
			}
			return (ParagraphTag)para.Tag;
		}

        /// <summary>
        /// Gets an empty run
        /// </summary>
        /// <returns>An empty run with the default tag</returns>
        Run emptyRun()
        {
            Run run = new Run();
            run.Tag = new FormatTag(Format.Default);
            return run;
        }

        /// <summary>
        /// Gets a run with text content and the default tag
        /// </summary>
        /// <param name="text">The text content of the run</param>
        /// <param name="fmt">The format of the run</param>
        /// <param name="fmtType">The format type of the run</param>
        /// <returns>A run with the given text, and a tag containing the format and format type</returns>
        Run filledRun(string text)
        {
            Run run = new Run(text);
            run.Tag = new FormatTag(Format.Default);
            return run;
        }

        /// <summary>
        /// Gets a run with content
        /// </summary>
        /// <param name="text">The text content of the run</param>
        /// <param name="fmt">The format of the run</param>
        /// <param name="fmtType">The format type of the run</param>
        /// <returns>A run with the given text, and a tag containing the format and format type</returns>
        Run filledRun(string text, Format fmt, FormatType fmtType)
        {
            Run run = new Run(text);
            run.Tag = new FormatTag(fmt, fmtType);
            return run;
        }

		Paragraph plainPara(Paragraph orig)
		{
			ParagraphTag pTag = getTag(orig);

			//return this paragraph, if plaintext
			if (pTag.IsPlainText) return orig;

			//correct for copied alternate error
			if (pTag.Alternate != null)
			{
				if (getTag(pTag.Alternate).Alternate != orig)
				{
					pTag.Alternate = null;
				}
			}

			if (pTag.Alternate == null)
			{
				//generate plaintext alternate, if needed
				//don't see how this could happen, but better safe ...
				pTag.Alternate = new Paragraph(
					filledRun(textBetween(orig.ContentStart, orig.ContentEnd)));
				pTag.Alternate.Tag = new ParagraphTag(true, orig);
			}

			//return plaintext alternate
			return pTag.Alternate;
		}

		const bool IGNORE_ALTERNATE = true;
		Paragraph formattedPara(Paragraph orig, bool ignoreAlternate = false)
		{
			ParagraphTag pTag = getTag(orig);
			
			//return this paragraph, if formatted
			if (!pTag.IsPlainText) return orig;

			//correct for copied alternate error
			if (pTag.Alternate != null)
			{
				if (getTag(pTag.Alternate).Alternate != orig)
				{
					pTag.Alternate = null;
				}
			}

			if (ignoreAlternate || pTag.Alternate == null)
			{
				//generate formatted alternate, if needed
				pTag.Alternate = formatPara(orig);
				pTag.Alternate.Tag = new ParagraphTag(false, orig);
			}

			//return formatted alternate
			return pTag.Alternate;
		}

		public void LogTextInput(object sender, TextCompositionEventArgs e)
		{
			//MessageBox.Show("Saw text entry with value \"" + e.Text + "\"");
			logText(e.Text);
		}

		public void LogKeyDown(object sender, KeyEventArgs e)
		{
			ModifierKeys modifiers = e.KeyboardDevice.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control);

			switch (e.Key)
			{
				case Key.Space:
					//MessageBox.Show("Saw key down with value <Space>");
					logText(" ");
					break;
				case Key.Return:
				case Key.LineFeed:
					//MessageBox.Show("Saw key down with value <" + e.Key + ">");
					log("New Paragraph {NEWPARA}");
					break;
				case Key.Home:
				case Key.End:
				case Key.Left:
				case Key.Right:
				case Key.Up:
				case Key.Down:
					//MessageBox.Show("Saw key down with value <" + e.Key + ">");
					logSelectionChanged(modifiersString(modifiers) + e.Key);
					break;
				case Key.Back:
					if (editField.Selection.IsEmpty) 
					{
						TextPointer p = editField.Selection.Start.GetNextInsertionPosition(LogicalDirection.Backward) 
							?? editField.Selection.Start;
						logDeletion(new TextRange(p, editField.Selection.Start).Text, "Back");
					}
					else logDeletion(editField.Selection.Text, "Back");
					break;
				case Key.Delete:
					if (editField.Selection.IsEmpty) 
					{
						TextPointer p = editField.Selection.Start.GetNextInsertionPosition(LogicalDirection.Forward)
							?? editField.Selection.Start;
						logDeletion(new TextRange(editField.Selection.Start, p).Text, "Delete");
					}
					else logDeletion(editField.Selection.Text, "Delete");
					break;
			}
		}

		string modifiersString(ModifierKeys keys)
		{	
			string s = "";

			if ((keys & ModifierKeys.Control) != ModifierKeys.None) s += "Ctrl+";
			if ((keys & ModifierKeys.Shift) != ModifierKeys.None) s += "Shift+";

			return s;
		}

		public void LogMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			logSelectionChanged("mouse");
		}

		string cleanNewlines(string s)
		{
			s = s.Replace("\r", "");
			return s.Replace("\n", "\\n");
		}

		void logSelectionChanged(string where)
		{
			log("selection changed {SELCHG:" + where + "}");
		}

		void logDeletion(string what, string how)
		{
			log("deleted |" + cleanNewlines(what) + "| {DEL:" + how + "}");
		}

		string textToLog = "";

		void logText(string s)
		{
			textToLog += s;
		}

		void flushTextLog()
		{
			TimeSpan span = DateTime.Now - LogStart;
			LogFile.WriteLine("[{0}]({1}:{2}.{3}) {4}",
					span.Ticks, (long)span.TotalMinutes, span.Seconds, span.Milliseconds,
					"inserted |" + cleanNewlines(textToLog) + "| {INS}");
			textToLog = "";
		}

		void log(String s)
		{
			TimeSpan span = DateTime.Now - LogStart;
			if (textToLog.Length > 0) flushTextLog();
			LogFile.WriteLine("[{0}]({1}:{2}.{3}) {4}", 
				span.Ticks, (long)span.TotalMinutes, span.Seconds, span.Milliseconds, 
				cleanNewlines(s));
			
		}

    }

	/*
	class SelectAdorner : Adorner
	{

		public static readonly DependencyProperty RectTopProperty =
			DependencyProperty.Register(
				"RectTop", typeof(double), typeof(SelectAdorner),
				new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
		public static readonly DependencyProperty RectLeftProperty =
			DependencyProperty.Register(
				"RectLeft", typeof(double), typeof(SelectAdorner),
				new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
		public static readonly DependencyProperty RectHeightProperty =
			DependencyProperty.Register(
				"RectHeight", typeof(double), typeof(SelectAdorner),
				new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
		public static readonly DependencyProperty RectWidthProperty =
			DependencyProperty.Register(
				"RectWidth", typeof(double), typeof(SelectAdorner),
				new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

		private double RectTop
		{
			get { return (double)GetValue(RectTopProperty); }
			set { SetValue(RectTopProperty, value); }
		}
		private double RectLeft
		{
			get { return (double)GetValue(RectLeftProperty); }
			set { SetValue(RectLeftProperty, value); }
		}
		private double RectHeight
		{
			get { return (double)GetValue(RectHeightProperty); }
			set { SetValue(RectHeightProperty, value); }
		}
		private double RectWidth
		{
			get { return (double)GetValue(RectWidthProperty); }
			set { SetValue(RectWidthProperty, value); }
		}

		private RichTextBox Adorned;
		
		public SelectAdorner(RichTextBox adorned, Paragraph first, Paragraph last)
			: base(adorned)
		{
			IsHitTestVisible = false;
			Adorned = adorned;
			update(first, last);
		}

		public void update(Paragraph first, Paragraph last)
		{
			double top = first.ContentStart.GetCharacterRect(LogicalDirection.Forward).Top;
			if (top < 0) top = 0;

			double left = 5;

			double height = last.ContentEnd.GetCharacterRect(LogicalDirection.Backward).Bottom;
			if (height > Adorned.Height - top) height = Adorned.Height - top;
			if (height < 0) height = 0;

			double width = Adorned.Width - 20;
			if (width < 0) width = 0;

			RectTop = top; RectLeft = left; RectHeight = height; RectWidth = width;
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			drawingContext.DrawRectangle(null, new Pen(Brushes.Navy, 1),
				new Rect(RectLeft, RectTop, RectWidth, RectHeight));
			
			base.OnRender(drawingContext);
		}
	}
	*/
}
