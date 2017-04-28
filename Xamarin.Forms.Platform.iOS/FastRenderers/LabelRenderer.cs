using System;
using System.ComponentModel;
using RectangleF = CoreGraphics.CGRect;
using SizeF = CoreGraphics.CGSize;
//using Xamarin.Forms;

#if __MOBILE__
using UIKit;
using NativeView = UIKit.UIView;
using NativeLabel = UIKit.UILabel;
using NativeViewController = UIKit.UIViewController;
using Xamarin.Forms.Platform.iOS;

namespace Xamarin.Forms.Platform.iOS.FastRenderers
#else
using AppKit;
using NativeView = AppKit.NSView;
using NativeLabel = AppKit.NSTextField;
using NativeViewController = AppKit.NSViewController;
using Xamarin.Forms.Platform.MacOS;

namespace Xamarin.Forms.Platform.MacOS.FastRenderers
#endif
{
	public class LabelRenderer : NativeLabel, IVisualElementRenderer
	{
		SizeRequest _perfectSize;

		bool _perfectSizeValid;

		readonly VisualElementRendererBridge _visualElementRendererBridge;

		public event EventHandler<VisualElementChangedEventArgs> ElementChanged;


		Label Label => Element as Label;

		NativeLabel Control => this as NativeLabel;

		public VisualElement Element { get; private set; }

		public NativeView NativeView => this;

		NativeViewController IVisualElementRenderer.ViewController => null;


		public LabelRenderer() : base()
		{
			_visualElementRendererBridge = new VisualElementRendererBridge(this);
		}


		void IVisualElementRenderer.SetElement(VisualElement element)
		{
			var oldElement = Element;
			Element = element;

			if (oldElement != null)
				oldElement.PropertyChanged -= OnElementPropertyChanged;

			if (element != null)
			{
				element.PropertyChanged += OnElementPropertyChanged;
			}

			ElementChanged?.Invoke(this, new VisualElementChangedEventArgs(oldElement, element));

			if (element != null)
			{
#if !__MOBILE__
				Control.Editable = false;
				Control.Bezeled = false;
				Control.DrawsBackground = false;
#endif

				UpdateText();
				UpdateLineBreakMode();
				UpdateAlignment();
			}

		}

		void IVisualElementRenderer.SetElementSize(Size size)
		{
			Layout.LayoutChildIntoBoundingRegion(Element, new Rectangle(Element.X, Element.Y, size.Width, size.Height));
		}

		SizeRequest IVisualElementRenderer.GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			if (!_perfectSizeValid)
			{
				_perfectSize = Control.GetSizeRequest(double.PositiveInfinity, double.PositiveInfinity);
				_perfectSize.Minimum = new Size(Math.Min(10, _perfectSize.Request.Width), _perfectSize.Request.Height);
				_perfectSizeValid = true;
			}

			var widthFits = widthConstraint >= _perfectSize.Request.Width;
			var heightFits = heightConstraint >= _perfectSize.Request.Height;

			if (widthFits && heightFits)
				return _perfectSize;

			var result = Control.GetSizeRequest(widthConstraint, heightConstraint);
			var tinyWidth = Math.Min(10, result.Request.Width);
			result.Minimum = new Size(tinyWidth, result.Request.Height);

			if (widthFits || Label.LineBreakMode == Xamarin.Forms.LineBreakMode.NoWrap)
				return result;

			bool containerIsNotInfinitelyWide = !double.IsInfinity(widthConstraint);

			if (containerIsNotInfinitelyWide)
			{
				bool textCouldHaveWrapped = Label.LineBreakMode == Xamarin.Forms.LineBreakMode.WordWrap || Label.LineBreakMode == Xamarin.Forms.LineBreakMode.CharacterWrap;
				bool textExceedsContainer = result.Request.Width > widthConstraint;

				if (textExceedsContainer || textCouldHaveWrapped)
				{
					var expandedWidth = Math.Max(tinyWidth, widthConstraint);
					result.Request = new Size(expandedWidth, result.Request.Height);
				}
			}

			return result;
		}

#if __MOBILE__
		public override void LayoutSubviews()
		{
			base.LayoutSubviews();
			_visualElementRendererBridge.LayoutSubviews();
#else
				public override void Layout()
				{
					base.Layout();
#endif


			SizeF fitSize = Control.SizeThatFits(Element.Bounds.Size.ToSizeF());
			nfloat labelHeight = (nfloat)Math.Min(Bounds.Height, fitSize.Height);
			switch (Label.VerticalTextAlignment)
			{
				case Xamarin.Forms.TextAlignment.Start:
					//fitSize = Control.SizeThatFits(Element.Bounds.Size.ToSizeF());
					//labelHeight = (nfloat)Math.Min(Bounds.Height, fitSize.Height);
					Control.Frame = new RectangleF(Element.X, Element.Y, (nfloat)Element.Width, labelHeight);
					break;
				case Xamarin.Forms.TextAlignment.Center:
					Control.Frame = new RectangleF(Element.X, Element.Y + (Element.Bounds.Height - labelHeight) / 2, (nfloat)Element.Width, (nfloat)Element.Height);
					break;
				case Xamarin.Forms.TextAlignment.End:
					nfloat yOffset = 0;
					//fitSize = Control.SizeThatFits(Element.Bounds.Size.ToSizeF());
					//labelHeight = (nfloat)Math.Min(Bounds.Height, fitSize.Height);
					yOffset = (nfloat)(Element.Height - labelHeight);
					Control.Frame = new RectangleF(Element.X, Element.Y + yOffset, (nfloat)Element.Width, labelHeight);
					break;
			}
		}

		protected virtual void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == Label.HorizontalTextAlignmentProperty.PropertyName)
				UpdateAlignment();
			else if (e.PropertyName == Label.VerticalTextAlignmentProperty.PropertyName)
				UpdateLayout();
			else if (e.PropertyName == Label.TextColorProperty.PropertyName)
				UpdateText();
			else if (e.PropertyName == Label.FontProperty.PropertyName)
				UpdateText();
			else if (e.PropertyName == Label.TextProperty.PropertyName)
				UpdateText();
			else if (e.PropertyName == Label.FormattedTextProperty.PropertyName)
				UpdateText();
			else if (e.PropertyName == Label.LineBreakModeProperty.PropertyName)
				UpdateLineBreakMode();
		}


		void UpdateAlignment()
		{
#if __MOBILE__
			Control.TextAlignment = Label.HorizontalTextAlignment.ToNativeTextAlignment();
#else
					Control.Alignment = Element.HorizontalTextAlignment.ToNativeTextAlignment();
#endif
		}

		void UpdateLineBreakMode()
		{
			_perfectSizeValid = false;
#if __MOBILE__
			switch (Label.LineBreakMode)
			{
				case Xamarin.Forms.LineBreakMode.NoWrap:
					Control.LineBreakMode = UILineBreakMode.Clip;
					Control.Lines = 1;
					break;
				case Xamarin.Forms.LineBreakMode.WordWrap:
					Control.LineBreakMode = UILineBreakMode.WordWrap;
					Control.Lines = 0;
					break;
				case Xamarin.Forms.LineBreakMode.CharacterWrap:
					Control.LineBreakMode = UILineBreakMode.CharacterWrap;
					Control.Lines = 0;
					break;
				case Xamarin.Forms.LineBreakMode.HeadTruncation:
					Control.LineBreakMode = UILineBreakMode.HeadTruncation;
					Control.Lines = 1;
					break;
				case Xamarin.Forms.LineBreakMode.MiddleTruncation:
					Control.LineBreakMode = UILineBreakMode.MiddleTruncation;
					Control.Lines = 1;
					break;
				case Xamarin.Forms.LineBreakMode.TailTruncation:
					Control.LineBreakMode = UILineBreakMode.TailTruncation;
					Control.Lines = 1;
					break;
			}
#else
					switch (Label.LineBreakMode)
					{
						case Xamarin.Forms.LineBreakMode.NoWrap:
							Control.LineBreakMode = NSLineBreakMode.Clipping;
							Control.MaximumNumberOfLines = 1;
							break;
						case Xamarin.Forms.LineBreakMode.WordWrap:
							Control.LineBreakMode = NSLineBreakMode.ByWordWrapping;
							Control.MaximumNumberOfLines = 0;
							break;
						case Xamarin.Forms.LineBreakMode.CharacterWrap:
							Control.LineBreakMode = NSLineBreakMode.CharWrapping;
							Control.MaximumNumberOfLines = 0;
							break;
						case Xamarin.Forms.LineBreakMode.HeadTruncation:
							Control.LineBreakMode = NSLineBreakMode.TruncatingHead;
							Control.MaximumNumberOfLines = 1;
							break;
						case Xamarin.Forms.LineBreakMode.MiddleTruncation:
							Control.LineBreakMode = NSLineBreakMode.TruncatingMiddle;
							Control.MaximumNumberOfLines = 1;
							break;
						case Xamarin.Forms.LineBreakMode.TailTruncation:
							Control.LineBreakMode = NSLineBreakMode.TruncatingTail;
							Control.MaximumNumberOfLines = 1;
							break;
					}
#endif
		}

		void UpdateText()
		{
			_perfectSizeValid = false;

			var values = Element.GetValues(Label.FormattedTextProperty, Label.TextProperty, Label.TextColorProperty);
			var formatted = values[0] as FormattedString;
			if (formatted != null)
			{
#if __MOBILE__
				Control.AttributedText = formatted.ToAttributed(Element, (Color)values[2]);
			}
			else
			{
				Control.Text = (string)values[1];
				// default value of color documented to be black in iOS docs
				Control.Font = Label.ToUIFont();
				Control.TextColor = ((Color)values[2]).ToUIColor(ColorExtensions.Black);
			}
#else
						Control.AttributedStringValue = formatted.ToAttributed(Element, (Color)values[2]);
					}
					else
					{
						Control.StringValue = (string)values[1] ?? "";
						// default value of color documented to be black in iOS docs
						Control.Font = Label.ToNSFont();
						Control.TextColor = ((Color)values[2]).ToNSColor(ColorExtensions.Black);
					}
#endif

			UpdateLayout();
		}

		void UpdateLayout()
		{
#if __MOBILE__
			LayoutSubviews();
#else
					Layout();
#endif
		}


	}


}
