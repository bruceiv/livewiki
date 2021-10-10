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
using System.Windows.Documents;

namespace LiveWiki
{

	[Flags]
	enum FormatType {
		Default = 0x0, 
		Bold = 0x1, 
		Italic = 0x2, 
		Heading = 0x4
	};

	struct Format
	{
		/// <summary>
		/// Converts typographic points into device independent pixels
		/// </summary>
		/// <param name="pts">Number of points</param>
		/// <returns>Number of pixels</returns>
		static double pt(double pts) { return 96 * pts / 72; }

		public static readonly FormatType DefaultFormats = FormatType.Default;
		public static readonly FontWeight DefaultFontWeight = FontWeights.Normal;
		public static readonly FontStyle DefaultFontStyle = FontStyles.Normal;
		public static readonly double DefaultFontSize = /*pt(11)*/ SystemFonts.MessageFontSize;
		public static TextDecorationCollection DefaultTextDecorations() { return new TextDecorationCollection(); }
		
		public static readonly Format Default =
			new Format(DefaultFormats, DefaultFontWeight, DefaultFontStyle, DefaultFontSize, DefaultTextDecorations());
		public static readonly Format Bold = 
			new Format(FormatType.Bold, FontWeights.Bold, DefaultFontStyle, DefaultFontSize, DefaultTextDecorations());
		public static readonly Format Italic = 
			new Format(FormatType.Italic, DefaultFontWeight, FontStyles.Italic, DefaultFontSize, DefaultTextDecorations());
		public static readonly Format Heading = 
			new Format(FormatType.Heading, DefaultFontWeight, DefaultFontStyle, DefaultFontSize + pt(2), 
				System.Windows.TextDecorations.Underline.Clone());

		public readonly FormatType Formats;
		public readonly FontWeight FontWeight;
		public readonly FontStyle FontStyle;
		public readonly double FontSize;
		public readonly TextDecorationCollection TextDecorations;
		
		private Format(FormatType formats, FontWeight fontWeight, FontStyle fontStyle, double fontSize, 
				TextDecorationCollection textDecorations)
		{
			Formats = formats;
			FontWeight = fontWeight;
			FontStyle = fontStyle;
			FontSize = fontSize;
			TextDecorations = textDecorations;
		}

		public Format(Format o)
		{
			Formats = o.Formats;
			FontWeight = o.FontWeight;
			FontStyle = o.FontStyle;
			FontSize = o.FontSize;
			TextDecorations = o.TextDecorations.Clone();
		}

		/// <summary>
		/// Composes two formats. If both have defined values, uses that of the first
		/// </summary>
		/// <param name="fmt1">The first format</param>
		/// <param name="fmt2">The second format</param>
		/// <returns>A format with fields set as follows: 
		///			undefined where both input formats are undefined, 
		///			the value of fmt2 where the fmt1 is undefined
		///			the value of fmt1 otherwise</returns>
		public static Format operator +(Format fmt1, Format fmt2)
		{
			FormatType formats = DefaultFormats | (fmt1.Formats | fmt2.Formats);
			FontWeight fontWeight = (fmt1.FontWeight == DefaultFontWeight) ? fmt2.FontWeight : fmt1.FontWeight;
			FontStyle fontStyle = (fmt1.FontStyle == DefaultFontStyle) ? fmt2.FontStyle : fmt1.FontStyle;
			double fontSize = (fmt1.FontSize == DefaultFontSize) ? fmt2.FontSize : fmt1.FontSize;
			TextDecorationCollection fmt1Decorations = fmt1.TextDecorations, 
				fmt2Decorations = fmt2.TextDecorations;
			if (fmt1Decorations == null) fmt1Decorations = new TextDecorationCollection();
			if (fmt2Decorations == null) fmt2Decorations = new TextDecorationCollection();
			TextDecorationCollection textDecorations = new TextDecorationCollection(
					DefaultTextDecorations().Union(fmt1Decorations.Union(fmt2Decorations))
				);
			return new Format(formats, fontWeight, fontStyle, fontSize, textDecorations);
		}

		/// <summary>
		/// Subtracts one format from another. If the first value is undefined, remains undefined.
		/// </summary>
		/// <param name="fmt1">The first format</param>
		/// <param name="fmt2">The second format</param>
		/// <returns>A format with fields set as follows: 
		///			undefined where the first format is undefined
		///			the value of fmt1 where fmt2 does not match it
		///			undefined if the two formats do match</returns>
		public static Format operator -(Format fmt1, Format fmt2)
		{
			FormatType formats = DefaultFormats | (fmt1.Formats & ~fmt2.Formats);
			FontWeight fontWeight = (fmt1.FontWeight == fmt2.FontWeight) ? DefaultFontWeight : fmt1.FontWeight;
			FontStyle fontStyle = (fmt1.FontStyle == fmt2.FontStyle) ? DefaultFontStyle : fmt1.FontStyle;
			double fontSize = (fmt1.FontSize == fmt2.FontSize) ? DefaultFontSize : fmt1.FontSize;
			TextDecorationCollection fmt1Decorations = fmt1.TextDecorations, 
				fmt2Decorations = fmt2.TextDecorations;
			if (fmt1Decorations == null) fmt1Decorations = new TextDecorationCollection();
			if (fmt2Decorations == null) fmt2Decorations = new TextDecorationCollection();
			TextDecorationCollection textDecorations = new TextDecorationCollection(
					DefaultTextDecorations().Union(fmt1Decorations.Except(fmt2Decorations))
				);
			return new Format(formats, fontWeight, fontStyle, fontSize, textDecorations);
		}

		public override bool Equals(object obj)
		{
			return (obj is Format) ? this.Equals((Format)obj) : false;
		}

		public bool Equals(Format o)
		{
			return
				Formats == o.Formats
				&& FontWeight == o.FontWeight
				&& FontStyle == o.FontStyle
				&& FontSize == o.FontSize
				&& TextDecorations.Equals(o.TextDecorations)
				;
		}

		public override int GetHashCode()
		{
			return (int)Formats;
		}

		public static bool IsApplied(FormatType fmt1, FormatType fmt2)
		{
			return (fmt1 & fmt2) == fmt2;
		}

		public static Format Toggle(Format fmt, FormatType fmtType)
		{
			return IsApplied(fmt.Formats, fmtType) ?
				fmt - GetFormat(fmtType) : fmt + GetFormat(fmtType);
		}

		public static Format GetFormat(FormatType type)
		{
			Format fmt = Default;
			if (IsApplied(type, FormatType.Bold)) fmt += Bold;
			if (IsApplied(type, FormatType.Italic)) fmt += Italic;
			if (IsApplied(type, FormatType.Heading)) fmt += Heading;
			return fmt;
		}
	}

}
