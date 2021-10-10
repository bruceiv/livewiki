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
using System.Windows.Input;

namespace LiveWiki
{
	class WikiCommand : RoutedCommand
	{
		public readonly string Markup;
		public readonly string Hint;
		public readonly Format Format;
		public readonly FormatType FormatType;

		public WikiCommand(String markup, String hint, Format format, FormatType formatType)
		{
			Markup = markup;
			Hint = hint;
			Format = format;
			FormatType = formatType;
		}
	}

	class WikiCommands
	{
		public const string BoldMarkup = "'''";
		public const string ItalicMarkup = "''";
		public const string HeadingMarkup = "==";

		public readonly char[] MarkupStarts = {'\'', '='};

		public static string getMarkup(FormatType type)
		{
			switch (type) {
				case FormatType.Bold:
					return BoldMarkup;
				case FormatType.Italic:
					return ItalicMarkup;
				case FormatType.Heading:
					return HeadingMarkup;
				default:
					return null;
			}
		}

		public static FormatType getFormatType(string markup)
		{
			switch (markup) {
				case BoldMarkup:
					return FormatType.Bold;
				case ItalicMarkup:
					return FormatType.Italic;
				case HeadingMarkup:
					return FormatType.Heading;
				default:
					return FormatType.Default;
			}
		}

		public static RoutedCommand WikiBold = new WikiCommand(BoldMarkup, "Bold text", Format.Bold, FormatType.Bold);
		public static RoutedCommand WikiItalic = new WikiCommand(ItalicMarkup, "Italic text", Format.Italic, FormatType.Italic);
		public static RoutedCommand WikiHeading = new WikiCommand(HeadingMarkup, "Heading text", Format.Heading, FormatType.Heading);
	}
}
