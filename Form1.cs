using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Atalasoft.Annotate;
using Atalasoft.Imaging.WinControls;
using Atalasoft.Imaging.Codec.Pdf;
using Atalasoft.Annotate.UI;
using Atalasoft.Imaging.Codec;
using System.Reflection;
using Atalasoft.Imaging;
using Atalasoft.Annotate.Exporters;


namespace DocumentAnnotationViewer
{
    public partial class Form1 : Form
    {
        #region FIELDS
        private AnnotationDefaults _defaultAnnotations = new AnnotationDefaults();
        private string _currentFile = "";
        private ImageList _annotationImages;
        #endregion FIELDS

        public Form1()
        {
            InitializeComponent();
            LoadAnnotationToolbarImages();
            EnableDisableMenuAndToolbarItems();
            AtalaDemos.HelperMethods.PopulateDecoders(RegisteredDecoders.Decoders);

            this.documentAnnotationViewer1.SelectedIndexChanged += new EventHandler(documentAnnotationViewer1_SelectedIndexChanged);

            this.documentAnnotationViewer1.Annotations.SelectionChanged += new EventHandler(documentAnnotationViewer1_Annotations_SelectionChanged);
            this.documentAnnotationViewer1.Annotations.Rotated += new AnnotationEventHandler(documentAnnotationViewer1_AnnotationRotated);
            this.documentAnnotationViewer1.Annotations.Resized += new AnnotationEventHandler(documentAnnotationViewer1_AnnotationResized);
            this.documentAnnotationViewer1.Annotations.Moved += new AnnotationEventHandler(documentAnnotationViewer1_AnnotationMoved);
            this.documentAnnotationViewer1.Annotations.AnnotationCreated += new AnnotationEventHandler(documentAnnotationViewer1_AnnotationCreated);

            this.documentAnnotationViewer1.SelectFirstPageOnOpen = true;
            this.documentAnnotationViewer1.Annotations.ClipToDocument = true;

            // We Need to add the ability to directly handle embedded annotations
            int PdfRes = 96;
            foreach (object decoder in RegisteredDecoders.Decoders)
            {
                if (decoder.GetType() == typeof(PdfDecoder))
                {
                    PdfDecoder pdfDec = decoder as PdfDecoder;
                    PdfRes = pdfDec.Resolution;
                    break;
                }
            }
            this.documentAnnotationViewer1.AnnotationDataProvider = new Atalasoft.Annotate.UI.EmbeddedAnnotationDataProvider(new PointF(PdfRes, PdfRes));
            this.documentAnnotationViewer1.CreatePdfAnnotationDataExporter += new CreatePdfAnnotationDataExporterHandler(documentAnnotationViewer1_CreatePdfAnnotationDataExporter);
      
        }

        Atalasoft.Annotate.Exporters.PdfAnnotationDataExporter documentAnnotationViewer1_CreatePdfAnnotationDataExporter(object sender, CreatePdfAnnotationDataExporterEventArgs e)
        {
            PdfAnnotationDataExporter exp = new PdfAnnotationDataExporter();
            exp.AlwaysEmbedAnnotationData = true;
            exp.OverwriteExistingAnnotations = true;
            return exp;
        }

        #region ImageControl Event Handlers
        /// <summary>
        /// Handles updating of the Status Strip to indicate current selected page (index +1)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void documentAnnotationViewer1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int currentPage = 0;
            if (this.documentAnnotationViewer1.ThumbnailControl.SelectedIndicies.Length > 0)
            {
                currentPage = this.documentAnnotationViewer1.ThumbnailControl.SelectedIndicies[0] + 1;
            }

            this.toolStripStatusPage.Text = "Page " + currentPage + " of " + this.documentAnnotationViewer1.Count.ToString();
        }
        #endregion ImageControl Event Handlers


        #region Annotation Event Handlers

        private void RubberStampTool_Click(object sender, EventArgs e)
        {
            Atalasoft.Annotate.UI.RubberStampAnnotation stamp = (Atalasoft.Annotate.UI.RubberStampAnnotation)_defaultAnnotations.GetAnnotation(AnnotationType.RubberStamp);
            stamp.Text = ((ToolStripMenuItem)sender).Text;
            this.documentAnnotationViewer1.Annotations.CreateAnnotation(stamp, Atalasoft.Annotate.CreateAnnotationMode.SingleClickCenter);
        }

        void documentAnnotationViewer1_Annotations_SelectionChanged(object sender, EventArgs e)
        {
            EnableDisableMenuAndToolbarItems();
        }

        private void documentAnnotationViewer1_AnnotationMoved(object sender, Atalasoft.Annotate.AnnotationEventArgs e)
        {
            EnableDisableMenuAndToolbarItems();
        }

        private void documentAnnotationViewer1_AnnotationResized(object sender, Atalasoft.Annotate.AnnotationEventArgs e)
        {
            EnableDisableMenuAndToolbarItems();
        }

        private void documentAnnotationViewer1_AnnotationRotated(object sender, Atalasoft.Annotate.AnnotationEventArgs e)
        {
            EnableDisableMenuAndToolbarItems();
        }

        private void documentAnnotationViewer1_UndoListChanged(object sender, EventArgs e)
        {
            EnableDisableMenuAndToolbarItems();
        }

        private void documentAnnotationViewer1_AnnotationCreated(object sender, Atalasoft.Annotate.AnnotationEventArgs e)
        {
            Atalasoft.Annotate.UI.TextAnnotation txt = e.Annotation as Atalasoft.Annotate.UI.TextAnnotation;
            if (txt != null)
                txt.EditMode = true;

            Atalasoft.Annotate.UI.CalloutAnnotation ca = e.Annotation as Atalasoft.Annotate.UI.CalloutAnnotation;
            if (ca != null)
                ca.EditMode = true;

            e.Annotation.Selected = true;
            e.Annotation.ContextMenuStrip = this.contextMenuStrip1;
            EnableDisableMenuAndToolbarItems();
        }

        private void documentAnnotationViewer1_SelectionRectangleChanged(object sender, Atalasoft.Imaging.WinControls.RubberbandEventArgs e)
        {
            Rectangle rc = this.documentAnnotationViewer1.ImageControl.Selection.Bounds;
            RectangleF selectionBounds = new RectangleF(rc.X, rc.Y, rc.Width, rc.Height);
            SwitchAnnotationAuthorMode(true);
            this.documentAnnotationViewer1.Annotations.SelectFromBounds(selectionBounds, true);
        }
        #endregion Annotation Event Handlers


        #region Misc Methods
        private void OpenImageFile()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = AtalaDemos.HelperMethods.CreateDialogFilter(true);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    this.documentAnnotationViewer1.Open(dlg.FileName, -1);
                    this._currentFile = dlg.FileName;
                }
            }
            if (this.documentAnnotationViewer1.Count > 0)
            {
                UpdateStatusBar();
            }
            EnableDisableMenuAndToolbarItems();

            int layers = this.documentAnnotationViewer1.Annotations.Layers.Count;
        }


        /// <summary>
        /// Updates the page x of y and file name items on the statusbar
        /// </summary>
        private void UpdateStatusBar()
        {
            // We could probably just force this to 1, but to be safe, let's keep it dynamic
            int currentPage = 0;
            if (this.documentAnnotationViewer1.ThumbnailControl.SelectedIndicies.Length > 0)
            {
                currentPage = this.documentAnnotationViewer1.ThumbnailControl.SelectedIndicies[0] + 1;
            }
            this.toolStripStatusPage.Text = "Page " + currentPage.ToString() + " of " + this.documentAnnotationViewer1.Count.ToString();
            this.toolStripStatusFile.Text = System.IO.Path.GetFileName(this._currentFile);
        }


        /// <summary>
        /// When called without a fileName, 
        /// we provide a SaveFileDialog, then pass along to the version that takes a fileName
        /// </summary>
        private void SaveImageFile()
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = AtalaDemos.HelperMethods.CreateDialogFilter(false);
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // slight change from the video...
                    SaveImageFile(dlg.FileName);
                    this._currentFile = dlg.FileName;
                    UpdateStatusBar();
                }
            }

        }


        /// <summary>
        /// Performs the file save... note that annotaitons are automatically embedded 
        /// if the target encoder supports embedded annotations
        /// </summary>
        /// <param name="fileName"></param>
        private void SaveImageFile(string fileName)
        {
            ImageEncoder enc = GetEncoder(Path.GetExtension(fileName));

            if (enc != null)
            {
                this.documentAnnotationViewer1.Save(fileName, enc);
            }
            else
            {
                MessageBox.Show("unable to determine correct encoder for file: " + fileName);
            }

        }


        /// <summary>
        /// Examines the file extension on fileName to return the correct ImageEncoder
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private ImageEncoder GetEncoder(string fileName)
        {
            string ext = Path.GetExtension(fileName);

            ImageEncoder returnEnc = null;

            if (ext != null)
            {
                switch (ext.ToLower())
                {
                    // Not using these
                    //case ".jpg":
                    //case ".jpeg":
                    //    returnEnc = new JpegEncoder();
                    //    break;
                    //case ".png":
                    //    returnEnc = new PngEncoder();
                    //    break;
                    //case ".gif":
                    //    returnEnc = new GifEncoder();
                    //    break;
                    //case ".bmp":
                    //    returnEnc = new BmpEncoder();
                    //    break;
                    case ".tif":
                    case ".tiff":
                        returnEnc = new TiffEncoder();
                        break;
                    case ".pdf":
                        returnEnc = new PdfEncoder();
                        break;
                    default:
                        returnEnc = null;
                        break;
                }
            }
            return returnEnc;
        }


        /// <summary>
        /// Cleans up the annotations/images and undos to let go of the current file
        /// </summary>
        /// <returns></returns>
        private bool CloseCurrentFile()
        {
            this.documentAnnotationViewer1.Clear();
            this.documentAnnotationViewer1.Annotations.UndoManager.Clear();
            return true;
        }


        private void SetContextMenu(Atalasoft.Annotate.UI.LayerAnnotation layer)
        {
            foreach (Atalasoft.Annotate.UI.AnnotationUI ann in layer.Items)
            {
                LayerAnnotation l = ann as LayerAnnotation;
                if (l != null)
                    SetContextMenu(l);
                else
                    ann.ContextMenuStrip = this.contextMenuStrip1;
            }
        }

        /// <summary>
        /// Used by the EmbeddedImageAnnotation and ReferencedImageAnnotation creation methods to get the filename to use
        /// </summary>
        /// <returns></returns>
        private string GetFilename()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "Images|*.jpg;*.png;*.tif;*.tiff;*.bmp;*.emf;*.wmf;*.gif";

                if (dlg.ShowDialog(this) == DialogResult.OK)
                    return dlg.FileName;
                else
                    return null;
            }
        }

        /// <summary>
        /// When printing, DocumentAnnotationViewer handles the annotations for you
        /// Just create a blank print document, provide it to the print dialog, then pass on to the Print() method
        /// </summary>
        private void PrintImages()
        {
            using (AnnotatePrintDocument printDoc = new AnnotatePrintDocument())
            {
                using (PrintDialog diag = new PrintDialog())
                {
                    diag.Document = printDoc;
                    if (diag.ShowDialog() == DialogResult.OK)
                    {
                        documentAnnotationViewer1.Print(printDoc);
                    }
                }
            }
        }

        private void EnableDisableMenuAndToolbarItems()
        {
            bool fileLoaded = this.documentAnnotationViewer1.Count > 0;
            this.saveAsToolStripMenuItem.Enabled = fileLoaded;
            this.printToolStripMenuItem.Enabled = fileLoaded;
            this.toolStripPrint.Enabled = fileLoaded;
            this.toolStripSave.Enabled = fileLoaded;
            this.toolStripAnnotations.Enabled = fileLoaded;
            this.toolStripPointer.Enabled = fileLoaded;
            this.toolStripSelection.Enabled = fileLoaded;
            this.toolStripZoomIn.Enabled = (fileLoaded && this.documentAnnotationViewer1.ImageControl.AutoZoom == Atalasoft.Imaging.WinControls.AutoZoomMode.None);
            this.toolStripZoomOut.Enabled = (fileLoaded && this.documentAnnotationViewer1.ImageControl.AutoZoom == Atalasoft.Imaging.WinControls.AutoZoomMode.None);

            bool annotationSelected = this.documentAnnotationViewer1.Annotations.ActiveAnnotation != null;
            this.cutToolStripMenuItem.Enabled = annotationSelected;
            this.copyToolStripMenuItem.Enabled = annotationSelected;
            this.toolStripCut.Enabled = annotationSelected;
            this.toolStripCopy.Enabled = annotationSelected;

            bool canPaste = this.documentAnnotationViewer1.Annotations.CanPaste();
            this.pasteToolStripMenuItem.Enabled = canPaste;
            this.toolStripPaste.Enabled = canPaste;

            bool canUndo = this.documentAnnotationViewer1.Annotations.UndoManager.UndoCount > 0;
            bool canRedo = this.documentAnnotationViewer1.Annotations.UndoManager.RedoCount > 0;
            this.undoToolStripMenuItem.Enabled = canUndo;
            this.redoToolStripMenuItem.Enabled = canRedo;
            this.toolStripUndo.Enabled = canUndo;
            this.toolStripRedo.Enabled = canRedo;
        }


        private void PerformUndo()
        {
            this.documentAnnotationViewer1.Annotations.UndoManager.Undo();
            EnableDisableMenuAndToolbarItems();
        }


        private void PerformRedo()
        {
            this.documentAnnotationViewer1.Annotations.UndoManager.Redo();
            EnableDisableMenuAndToolbarItems();
        }


        private void PerformCut()
        {
            this.documentAnnotationViewer1.Annotations.Cut();
            EnableDisableMenuAndToolbarItems();
        }


        private void PerformCopy()
        {
            this.documentAnnotationViewer1.Annotations.Copy();
            EnableDisableMenuAndToolbarItems();
        }


        private void PerformPaste()
        {
            this.documentAnnotationViewer1.Annotations.Paste();
            EnableDisableMenuAndToolbarItems();
        }


        private Atalasoft.Annotate.AnnotationBrush GetAnnotationBrush(AnnotationUI annotation, string propertyName)
        {
            // Use reflection to see if there is a brush.
            Type at = annotation.GetType();
            System.Reflection.PropertyInfo info = at.GetProperty(propertyName);
            if (info == null) return null;

            return info.GetValue(annotation, null) as Atalasoft.Annotate.AnnotationBrush;
        }


        private Atalasoft.Annotate.AnnotationPen GetAnnotationPen(AnnotationUI annotation, string propertyName)
        {
            Type at = annotation.GetType();
            System.Reflection.PropertyInfo info = at.GetProperty(propertyName);
            if (info == null) return null;

            return info.GetValue(annotation, null) as Atalasoft.Annotate.AnnotationPen;
        }


        private Color GetColor(Color current)
        {
            using (ColorDialog dlg = new ColorDialog())
            {
                dlg.Color = current;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    return dlg.Color;
                else
                    return Color.Empty;
            }
        }

        private void UpdateAnnotationDefault(AnnotationUI annotation)
        {
            if (String.IsNullOrEmpty(annotation.Data.Name))
                return;

            Type aType = typeof(AnnotationType);
            if (Enum.IsDefined(aType, annotation.Data.Name))
            {
                AnnotationType at = (AnnotationType)Enum.Parse(aType, annotation.Data.Name);
                this._defaultAnnotations.UpdateAnnotation(at, annotation);
            }
        }

        private void SwitchAnnotationAuthorMode(bool authoring)
        {
            if (authoring)
            {
                this.documentAnnotationViewer1.ImageControl.MouseTool = MouseToolType.None;
                this.documentAnnotationViewer1.Annotations.InteractMode = AnnotateInteractMode.Author;
            }
            else
            {
                this.documentAnnotationViewer1.Annotations.InteractMode = AnnotateInteractMode.None;

                // The first time we switch the MouseTool to Selection we have to add the Changed event handler.
                bool addEvent = this.documentAnnotationViewer1.ImageControl.Selection == null;
                this.documentAnnotationViewer1.ImageControl.MouseTool = MouseToolType.Selection;
                if (addEvent)
                {
                    this.documentAnnotationViewer1.ImageControl.Selection.Changed += new RubberbandEventHandler(this.documentAnnotationViewer1_SelectionRectangleChanged);
                    this.documentAnnotationViewer1.ImageControl.Selection.ClickLock = false;
                }

                this.documentAnnotationViewer1.ImageControl.Selection.SetDisplayBounds(RectangleF.Empty);
            }

            this.documentAnnotationViewer1.ImageControl.Selection.Visible = !authoring;
            this.toolStripSelection.Checked = !authoring;
            this.toolStripPointer.Checked = authoring;
            this.toolStripAnnotations.Enabled = authoring;
        }


        private void SetZoomMode(AutoZoomMode mode)
        {
            this.documentAnnotationViewer1.ImageControl.AutoZoom = mode;

            if (mode == Atalasoft.Imaging.WinControls.AutoZoomMode.None)
            {
                this.documentAnnotationViewer1.ImageControl.Zoom = 1.0;
                this.toolStripZoomIn.Enabled = true;
                this.toolStripZoomOut.Enabled = true;
            }
            else
            {
                this.toolStripZoomIn.Enabled = false;
                this.toolStripZoomOut.Enabled = false;
            }
        }
        #endregion Misc Methods


        #region File Menu

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenImageFile();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveImageFile();
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrintImages();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CloseCurrentFile()) this.Close();
        }

        #endregion


        #region Edit Menu

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PerformUndo();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PerformRedo();
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PerformCut();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PerformCopy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PerformPaste();
        }

        #endregion


        #region Help Menu
        private void helpAboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AtalaDemos.AboutBox.About aboutBox = new AtalaDemos.AboutBox.About("About Atalasoft DotImage Document Annotation Viewer Demo",
                "DotImage Document Annotation Viewer Demo");
            aboutBox.Description = "The Document Annotation Viewer Demo demonstrates how use our DocumentAnnotationViewer control. \r\n\r\nThis demo should be used to gain a basic understanding of how the DotImage DocumentAnnotationViewer functions. \r\n\r\nThe demo allows you to open various supported image files, automatically loading any supported embedded annotations. It also allows the creation / editing of various annotation types and saving out the resulting file with annotaions being embedded for supported formats. Additionally, it shows the ease of use of the built in undo/redo manager, as well as cut, copy, and paste of annotations, and even full document printing with annotations.  Requires DotImage license. Optionally, requires PdfRasterizer license in order to open/read PDF files.";
            aboutBox.ShowDialog();
        }
        #endregion Help Menu


        #region Tool Strips

        /// <summary>
        /// ClickHandlers for the main tool strip
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMain_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem.Text)
            {
                case "Open":
                    OpenImageFile();
                    break;
                case "Save":
                    SaveImageFile();
                    break;
                case "Print":
                    PrintImages();
                    break;
                case "Undo":
                    PerformUndo();
                    break;
                case "Redo":
                    PerformRedo();
                    break;
                case "Cut":
                    PerformCut();
                    break;
                case "Copy":
                    PerformCopy();
                    break;
                case "Paste":
                    PerformPaste();
                    break;
                case "Pointer":
                    if (!this.toolStripPointer.Checked)
                        SwitchAnnotationAuthorMode(true);
                    break;
                case "Selection Tool":
                    if (!this.toolStripSelection.Checked)
                        SwitchAnnotationAuthorMode(false);
                    break;
                case "Zoom In":
                    this.documentAnnotationViewer1.ImageControl.Zoom += 0.1;
                    break;
                case "Zoom Out":
                    this.documentAnnotationViewer1.ImageControl.Zoom -= 0.1;
                    break;
            }
        }


        /// <summary>
        /// Click handlers for the annotation Tool Strip
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripAnnotations_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            this.documentAnnotationViewer1.Annotations.ClearSelection();

            switch (e.ClickedItem.Text)
            {
                case "Rectangle":
                    this.documentAnnotationViewer1.Annotations.CreateAnnotation(_defaultAnnotations.GetAnnotation(AnnotationType.Rectangle));
                    break;
                case "Ellipse":
                    this.documentAnnotationViewer1.Annotations.CreateAnnotation(_defaultAnnotations.GetAnnotation(AnnotationType.Ellipse));
                    break;
                case "Single Line":
                    this.documentAnnotationViewer1.Annotations.CreateAnnotation(_defaultAnnotations.GetAnnotation(AnnotationType.Line));
                    break;
                case "Connected Lines":
                    this.documentAnnotationViewer1.Annotations.CreateAnnotation(_defaultAnnotations.GetAnnotation(AnnotationType.Lines));
                    break;
                case "Freehand":
                    this.documentAnnotationViewer1.Annotations.CreateAnnotation(_defaultAnnotations.GetAnnotation(AnnotationType.Freehand));
                    break;
                case "Polygon":
                    this.documentAnnotationViewer1.Annotations.CreateAnnotation(_defaultAnnotations.GetAnnotation(AnnotationType.Polygon));
                    break;
                case "Highlighter":
                    this.documentAnnotationViewer1.Annotations.CreateAnnotation(_defaultAnnotations.GetAnnotation(AnnotationType.RectangleHighlighter));
                    break;
                case "Freehand Highlighter":
                    this.documentAnnotationViewer1.Annotations.CreateAnnotation(_defaultAnnotations.GetAnnotation(AnnotationType.FreehandHighlighter));
                    break;
                case "Embedded Image":
                    string eFile = GetFilename();
                    if (eFile != null)
                    {
                        Atalasoft.Annotate.UI.EmbeddedImageAnnotation ann = _defaultAnnotations.GetAnnotation(AnnotationType.EmbeddedImage) as Atalasoft.Annotate.UI.EmbeddedImageAnnotation;
                        ann.Image = new Atalasoft.Annotate.AnnotationImage(eFile);
                        this.documentAnnotationViewer1.Annotations.CreateAnnotation(ann);
                    }
                    break;
                case "Referenced Image":
                    string rFile = GetFilename();
                    if (rFile != null)
                    {
                        Atalasoft.Annotate.UI.ReferencedImageAnnotation rann = _defaultAnnotations.GetAnnotation(AnnotationType.ReferencedImage) as Atalasoft.Annotate.UI.ReferencedImageAnnotation;
                        rann.FileName = rFile;
                        this.documentAnnotationViewer1.Annotations.CreateAnnotation(rann);
                    }
                    break;
                case "Text":
                    this.documentAnnotationViewer1.Annotations.CreateAnnotation(_defaultAnnotations.GetAnnotation(AnnotationType.Text));
                    break;
                case "Callout":
                    this.documentAnnotationViewer1.Annotations.CreateAnnotation(_defaultAnnotations.GetAnnotation(AnnotationType.Callout));
                    break;
                case "Sticky Note":
                    this.documentAnnotationViewer1.Annotations.CreateAnnotation(_defaultAnnotations.GetAnnotation(AnnotationType.StickyNote), Atalasoft.Annotate.CreateAnnotationMode.SingleClickCenter);
                    break;
                case "Rubber Stamp":
                    this.documentAnnotationViewer1.Annotations.CreateAnnotation(_defaultAnnotations.GetAnnotation(AnnotationType.RubberStamp), Atalasoft.Annotate.CreateAnnotationMode.SingleClickCenter);
                    break;
                case "Redaction":
                    this.documentAnnotationViewer1.Annotations.CreateAnnotation(_defaultAnnotations.GetAnnotation(AnnotationType.Redaction));
                    break;
            }
        }


        /// <summary>
        /// Initializes the AnnotationToolstrip icons and loads them
        /// </summary>
        private void LoadAnnotationToolbarImages()
        {
            // Use the images provided within DotAnnotate.
            _annotationImages = new ImageList();
            _annotationImages.ImageSize = new Size(16, 16);
            _annotationImages.ColorDepth = ColorDepth.Depth32Bit;
            _annotationImages.Images.Add(AddIcon(Atalasoft.Annotate.Icons.AnnotateIcon.Callout, Atalasoft.Annotate.Icons.AnnotateIconSize.Size16));
            _annotationImages.Images.Add(AddIcon(Atalasoft.Annotate.Icons.AnnotateIcon.Ellipse, Atalasoft.Annotate.Icons.AnnotateIconSize.Size16));
            _annotationImages.Images.Add(AddIcon(Atalasoft.Annotate.Icons.AnnotateIcon.Freehand, Atalasoft.Annotate.Icons.AnnotateIconSize.Size16));
            _annotationImages.Images.Add(AddIcon(Atalasoft.Annotate.Icons.AnnotateIcon.FreehandHighlighter, Atalasoft.Annotate.Icons.AnnotateIconSize.Size16));
            _annotationImages.Images.Add(AddIcon(Atalasoft.Annotate.Icons.AnnotateIcon.RectangleHighlighter, Atalasoft.Annotate.Icons.AnnotateIconSize.Size16));
            _annotationImages.Images.Add(AddIcon(Atalasoft.Annotate.Icons.AnnotateIcon.EmbeddedImage, Atalasoft.Annotate.Icons.AnnotateIconSize.Size16));
            _annotationImages.Images.Add(AddIcon(Atalasoft.Annotate.Icons.AnnotateIcon.Line, Atalasoft.Annotate.Icons.AnnotateIconSize.Size16));
            _annotationImages.Images.Add(AddIcon(Atalasoft.Annotate.Icons.AnnotateIcon.Lines, Atalasoft.Annotate.Icons.AnnotateIconSize.Size16));
            _annotationImages.Images.Add(AddIcon(Atalasoft.Annotate.Icons.AnnotateIcon.StickyNote, Atalasoft.Annotate.Icons.AnnotateIconSize.Size16));
            _annotationImages.Images.Add(AddIcon(Atalasoft.Annotate.Icons.AnnotateIcon.Polygon, Atalasoft.Annotate.Icons.AnnotateIconSize.Size16));
            _annotationImages.Images.Add(AddIcon(Atalasoft.Annotate.Icons.AnnotateIcon.Rectangle, Atalasoft.Annotate.Icons.AnnotateIconSize.Size16));
            _annotationImages.Images.Add(AddIcon(Atalasoft.Annotate.Icons.AnnotateIcon.Redact, Atalasoft.Annotate.Icons.AnnotateIconSize.Size16));
            _annotationImages.Images.Add(AddIcon(Atalasoft.Annotate.Icons.AnnotateIcon.ReferencedImage, Atalasoft.Annotate.Icons.AnnotateIconSize.Size16));
            _annotationImages.Images.Add(AddIcon(Atalasoft.Annotate.Icons.AnnotateIcon.RubberStamp, Atalasoft.Annotate.Icons.AnnotateIconSize.Size16));
            _annotationImages.Images.Add(AddIcon(Atalasoft.Annotate.Icons.AnnotateIcon.Text, Atalasoft.Annotate.Icons.AnnotateIconSize.Size16));

            this.toolStripAnnotations.ImageList = _annotationImages;
            this.toolStripCallout.ImageIndex = 0;
            this.toolStripEllipse.ImageIndex = 1;
            this.toolStripFreehand.ImageIndex = 2;
            this.toolStripFreehandHighlighter.ImageIndex = 3;
            this.toolStripHighlighter.ImageIndex = 4;
            this.toolStripImage.ImageIndex = 5;
            this.toolStripLine.ImageIndex = 6;
            this.toolStripLines.ImageIndex = 7;
            this.toolStripNote.ImageIndex = 8;
            this.toolStripPolygon.ImageIndex = 9;
            this.toolStripRectangle.ImageIndex = 10;
            this.toolStripRedaction.ImageIndex = 11;
            this.toolStripReferencedImage.ImageIndex = 12;
            this.toolStripRubberStamp.ImageIndex = 13;
            this.toolStripText.ImageIndex = 14;
        }

        private Image AddIcon(Atalasoft.Annotate.Icons.AnnotateIcon annotateIcon, Atalasoft.Annotate.Icons.AnnotateIconSize annotateIconSize)
        {
            Image img = Atalasoft.Annotate.Icons.IconResource.ExtractAnnotationIcon(annotateIcon, annotateIconSize);
            if (img == null)
            {
                Assembly assm = Assembly.LoadFrom(@"Atalasoft.dotImage.dll");
                if (assm != null)
                {
                    System.IO.Stream stream = assm.GetManifestResourceStream("Atalasoft.Imaging.Annotate.Icons._" + annotateIconSize.ToString().Substring(4) + "." + annotateIcon.ToString() + ".png");
                    img = System.Drawing.Image.FromStream(stream);
                }
                if (img == null)
                {
                    if (annotateIconSize.ToString() == "size16")
                        return new AtalaImage(16, 16, PixelFormat.Pixel24bppBgr, Color.White).ToBitmap();
                    if (annotateIconSize.ToString() == "size24")
                        return new AtalaImage(24, 24, PixelFormat.Pixel24bppBgr, Color.White).ToBitmap();
                    if (annotateIconSize.ToString() == "size32")
                        return new AtalaImage(32, 32, PixelFormat.Pixel24bppBgr, Color.White).ToBitmap();
                }
            }
            return img;
        }
        #endregion


        #region Status bar menu

        private void fitToHeightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetZoomMode(Atalasoft.Imaging.WinControls.AutoZoomMode.FitToHeight);
        }

        private void fitToWidthToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetZoomMode(Atalasoft.Imaging.WinControls.AutoZoomMode.FitToWidth);
        }

        private void bestFitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetZoomMode(Atalasoft.Imaging.WinControls.AutoZoomMode.BestFitShrinkOnly);
        }

        private void noneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetZoomMode(Atalasoft.Imaging.WinControls.AutoZoomMode.None);
        }

        #endregion


        #region Context Menu

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            // Enable/disable menu items based on the annotation.
            Atalasoft.Annotate.UI.AnnotationUI annotation = this.documentAnnotationViewer1.Annotations.ActiveAnnotation;
            Type at = annotation.GetType();

            borderColorToolStripMenuItem.Enabled = (at.GetProperty("Outline") != null);
            fillColorToolStripMenuItem.Enabled = (at.GetProperty("Fill") != null && at.Name != "RubberStampAnnotation");
            textColorToolStripMenuItem.Enabled = (at.GetProperty("FontBrush") != null);
            fontToolStripMenuItem.Enabled = (at.GetProperty("Font") != null);
        }


        private void borderColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Atalasoft.Annotate.AnnotationPen pen = GetAnnotationPen(this.documentAnnotationViewer1.Annotations.ActiveAnnotation, "Outline");
            if (pen == null) return;

            Color clr = GetColor(pen.Color);
            if (clr != Color.Empty)
            {
                if (pen.Brush != null && pen.Brush.FillType == Atalasoft.Annotate.FillType.Hatch)
                    pen.Brush.HatchForeColor = clr;
                else
                    pen.Color = clr;
            }
            UpdateAnnotationDefault(this.documentAnnotationViewer1.Annotations.ActiveAnnotation);
        }


        private void fillColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Atalasoft.Annotate.AnnotationBrush brush = GetAnnotationBrush(this.documentAnnotationViewer1.Annotations.ActiveAnnotation, "Fill");
            if (brush == null) return;

            Color clr = GetColor(brush.Color);
            if (clr != Color.Empty) brush.Color = clr;
            UpdateAnnotationDefault(this.documentAnnotationViewer1.Annotations.ActiveAnnotation);
            //TODO: remove this
            //ProcessThumbnail(this._currentFrame);
        }


        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Atalasoft.Annotate.UI.AnnotationUI annotation = this.documentAnnotationViewer1.Annotations.ActiveAnnotation;
            Type at = annotation.GetType();
            System.Reflection.PropertyInfo info = at.GetProperty("Font");
            if (info == null) return;

            Atalasoft.Annotate.AnnotationFont font = info.GetValue(annotation, null) as Atalasoft.Annotate.AnnotationFont;
            if (font == null) return;

            FontStyle fontStyle = FontStyle.Regular;
            if (font.Bold) fontStyle = FontStyle.Bold;
            if (font.Italic) fontStyle |= FontStyle.Italic;
            if (font.Strikeout) fontStyle |= FontStyle.Strikeout;
            if (font.Underline) fontStyle |= FontStyle.Underline;

            using (FontDialog dlg = new FontDialog())
            {
                dlg.Font = new Font(font.Name, font.Size, fontStyle);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    font = new Atalasoft.Annotate.AnnotationFont(dlg.Font.Name, dlg.Font.Size, dlg.Font.Bold, dlg.Font.Italic, dlg.Font.Underline, dlg.Font.Strikeout);
                    info.SetValue(annotation, font, null);
                }
            }
            UpdateAnnotationDefault(annotation);
        }


        private void textColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Atalasoft.Annotate.AnnotationBrush brush = GetAnnotationBrush(this.documentAnnotationViewer1.Annotations.ActiveAnnotation, "FontBrush");
            if (brush == null) return;

            Color clr = GetColor(brush.Color);
            if (clr != Color.Empty)
            {
                if (brush.FillType == Atalasoft.Annotate.FillType.Hatch)
                    brush.HatchForeColor = clr;
                else
                    brush.Color = clr;
            }
            UpdateAnnotationDefault(this.documentAnnotationViewer1.Annotations.ActiveAnnotation);
        }
        #endregion Context Menu

    }
}