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
using System.Windows.Documents;
using System.Runtime.Serialization;
using System.Windows.Media;

namespace LiveWiki
{
	//[Serializable]
	class FormatTag //: ISerializable
	{
		public Format Format;
		public FormatType MarkupType;
		public bool ShowFormatting;
		
		public FormatTag(Format format, FormatType markupType = FormatType.Default, bool showFormatting = true)
		{
			Format = format;
			MarkupType = markupType;
			ShowFormatting = showFormatting;
		}

		/*
		//deserialization constructor
		protected FormatTag(SerializationInfo info, StreamingContext ctx)
		{
			FormatType fType = (FormatType)info.GetInt32("fmts");
			Format = Format.Default;
			if ((fType & FormatType.Bold) == FormatType.Bold) Format += Format.Bold;
			if ((fType & FormatType.Italic) == FormatType.Italic) Format += Format.Italic;
			if ((fType & FormatType.Heading) == FormatType.Heading) Format += Format.Heading;
			MarkupType = (FormatType)info.GetInt32("mtype");
		}

		//serialization method
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext ctx)
		{
			info.AddValue("fmts", (int)Format.Formats);
			info.AddValue("mtype", (int)MarkupType);
		}
		*/

		public static void Apply(FormatTag fmt, Run run)
		{
			//applies the markup format for markup tags, text format for normal tags
			switch (fmt.MarkupType) {
				case FormatType.Bold:
					apply(Format.Bold, run);
					run.Foreground = Brushes.Gray;
					break;
				case FormatType.Italic:
					apply(Format.Italic, run);
					run.Foreground = Brushes.Gray;
					break;
				case FormatType.Heading:
					apply(Format.Heading, run);
					run.Foreground = Brushes.Gray;
					break;
				default:
					apply(fmt.Format, run);
					run.Foreground = Brushes.Black;
					break;
			}
		}
		
		static void apply(Format fmt, Run run)
		{
			run.FontWeight = fmt.FontWeight;
			run.FontStyle = fmt.FontStyle;
			run.FontSize = fmt.FontSize;
			run.TextDecorations = fmt.TextDecorations;
		}
	}
}
