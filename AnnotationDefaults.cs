using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Reflection;

namespace DocumentAnnotationViewer
{
    class AnnotationDefaults
    {
        private System.Collections.Hashtable _table;

        public AnnotationDefaults()
        {
            _table = new System.Collections.Hashtable();

            _table.Add(AnnotationType.Ellipse, new Atalasoft.Annotate.UI.EllipseAnnotation(new Atalasoft.Annotate.AnnotationBrush(Color.Red), new Atalasoft.Annotate.AnnotationPen(Color.Red)));
            _table.Add(AnnotationType.EmbeddedImage, new Atalasoft.Annotate.UI.EmbeddedImageAnnotation());
            _table.Add(AnnotationType.Freehand, new Atalasoft.Annotate.UI.FreehandAnnotation(new Atalasoft.Annotate.AnnotationPen(Color.Blue, 4)));
            _table.Add(AnnotationType.Lines, new Atalasoft.Annotate.UI.LinesAnnotation());
            _table.Add(AnnotationType.Polygon, new Atalasoft.Annotate.UI.PolygonAnnotation(new Atalasoft.Annotate.AnnotationPen(Color.Black), new Atalasoft.Annotate.AnnotationBrush(Color.Green)));
            _table.Add(AnnotationType.Rectangle, new Atalasoft.Annotate.UI.RectangleAnnotation(new Atalasoft.Annotate.AnnotationBrush(Color.Orange), new Atalasoft.Annotate.AnnotationPen(Color.Silver)));
            _table.Add(AnnotationType.ReferencedImage, new Atalasoft.Annotate.UI.ReferencedImageAnnotation());
            _table.Add(AnnotationType.Text, new Atalasoft.Annotate.UI.TextAnnotation("", new Atalasoft.Annotate.AnnotationFont("Arial", 12), new Atalasoft.Annotate.AnnotationBrush(Color.Black), new Atalasoft.Annotate.AnnotationBrush(Color.Gainsboro), new Atalasoft.Annotate.AnnotationPen(Color.Black)));
            _table.Add(AnnotationType.Redaction, new Atalasoft.Annotate.UI.RectangleAnnotation(new Atalasoft.Annotate.AnnotationBrush(Color.Black), null));

            // Callout
            Atalasoft.Annotate.AnnotationPen leader = new Atalasoft.Annotate.AnnotationPen(Color.Black, 2);
            leader.EndCap = new Atalasoft.Annotate.AnnotationLineCap(Atalasoft.Annotate.AnnotationLineCapStyle.Arrow, new SizeF(15, 15));
            _table.Add(AnnotationType.Callout, new Atalasoft.Annotate.UI.CalloutAnnotation("", new Atalasoft.Annotate.AnnotationFont("Times New Roman", 12), new Atalasoft.Annotate.AnnotationBrush(Color.Black), 4, new Atalasoft.Annotate.AnnotationBrush(Color.White), new Atalasoft.Annotate.AnnotationPen(Color.Black, 2), leader, 10));
            
            // Line
            Atalasoft.Annotate.AnnotationPen textOutline = new Atalasoft.Annotate.AnnotationPen(Color.Black);
            textOutline.EndCap = new Atalasoft.Annotate.AnnotationLineCap(Atalasoft.Annotate.AnnotationLineCapStyle.FilledArrow, new SizeF(15, 15));
            _table.Add(AnnotationType.Line, new Atalasoft.Annotate.UI.LineAnnotation(textOutline));
            
            // Rubberstamp
            Atalasoft.Annotate.UI.RubberStampAnnotation stamp = new Atalasoft.Annotate.UI.RubberStampAnnotation();
            stamp.Data.Size = new SizeF(400, 110);
            stamp.Data.CanMirror = false;
            stamp.Text = "DRAFT";
            _table.Add(AnnotationType.RubberStamp, stamp);

            // Sticky Note
            Atalasoft.Annotate.UI.TextAnnotation sticky = new Atalasoft.Annotate.UI.TextAnnotation("", new Atalasoft.Annotate.AnnotationFont("Arial", 12), new Atalasoft.Annotate.AnnotationBrush(Color.Black), new Atalasoft.Annotate.AnnotationBrush(SystemColors.Info), new Atalasoft.Annotate.AnnotationPen(Color.Black, 1));
            sticky.Data.Size = new SizeF(200, 120);
            sticky.Shadow = new Atalasoft.Annotate.AnnotationBrush(Color.FromArgb(120, Color.Silver));
            sticky.ShadowOffset = new PointF(5, 5);
            _table.Add(AnnotationType.StickyNote, sticky);
            
            // Rectangle Highlighter
            Atalasoft.Annotate.UI.RectangleAnnotation rc = new Atalasoft.Annotate.UI.RectangleAnnotation(new Atalasoft.Annotate.AnnotationBrush(Color.Yellow), null);
            rc.Translucent = true;
            _table.Add(AnnotationType.RectangleHighlighter, rc);

            // Freehand Highlighter
            Atalasoft.Annotate.UI.FreehandAnnotation fh = new Atalasoft.Annotate.UI.FreehandAnnotation(new Atalasoft.Annotate.AnnotationPen(Color.Yellow, 20));
            fh.Translucent = true;
            fh.LineType = Atalasoft.Annotate.FreehandLineType.Curves;
            _table.Add(AnnotationType.FreehandHighlighter, fh);
        }

        public Atalasoft.Annotate.UI.AnnotationUI GetAnnotation(AnnotationType type)
        {
            Atalasoft.Annotate.UI.AnnotationUI ann = ((Atalasoft.Annotate.UI.AnnotationUI)_table[type]).Clone();
            ann.Data.Name = type.ToString();
            return ann;
        }

        public void UpdateAnnotation(AnnotationType type, Atalasoft.Annotate.UI.AnnotationUI annotation)
        {
            Atalasoft.Annotate.UI.AnnotationUI newAnn = (Atalasoft.Annotate.UI.AnnotationUI)_table[type];

            Type at = newAnn.GetType();
            CopyProperty("Fill", at, annotation, newAnn);
            CopyProperty("Outline", at, annotation, newAnn);
            CopyProperty("Font", at, annotation, newAnn);
            CopyProperty("FontBrush", at, annotation, newAnn);

            if (type != AnnotationType.RubberStamp && type != AnnotationType.StickyNote)
                newAnn.Size = SizeF.Empty;

            _table[type] = newAnn;
        }

        private void CopyProperty(string propertyName, Type type, Atalasoft.Annotate.UI.AnnotationUI annotation, Atalasoft.Annotate.UI.AnnotationUI newAnnotation)
        {
            PropertyInfo info = type.GetProperty(propertyName);
            if (info == null) return;

            object val = info.GetValue(annotation, null);
            if (val == null) return;

            Type objType = val.GetType();
            MethodInfo cloneMethod = objType.GetMethod("Clone");

            info.SetValue(newAnnotation, cloneMethod.Invoke(val, null), null);
        }
    }

    public enum AnnotationType
    {
        Callout,
        Line,
        Lines,
        Ellipse,
        Rectangle,
        Freehand,
        RectangleHighlighter,
        FreehandHighlighter,
        EmbeddedImage,
        ReferencedImage,
        Polygon,
        Text,
        StickyNote,
        Redaction,
        RubberStamp
    }
}
