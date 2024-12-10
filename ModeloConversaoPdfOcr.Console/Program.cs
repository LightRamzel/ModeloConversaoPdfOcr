using System.IO;
using Tesseract;
using PdfiumViewer;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace ModeloConversaoPdfOcr.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var originalPdfPath = @"C:\Users\lucia\OneDrive\Área de Trabalho\Dskp\Base PDF\Arquivos Em Pastas\50000312720168210029\02911600032306.PDF";
            var searchablePdfPath = @"C:\Users\lucia\OneDrive\Área de Trabalho\Dskp\Base PDF\Arquivos Em Pastas\50000312720168210029\02911600032306_teste.PDF";

            string tessdataPath = "./tessdata";
            string language = "por";

            // Agora, realizar OCR em cada página do PDF de saída e adicionar o texto OCR como uma camada extra
            using (var pdfDocument = PdfiumViewer.PdfDocument.Load(originalPdfPath))
            using (var reader = new PdfReader(originalPdfPath))
            using (var fileStream = new FileStream(searchablePdfPath, FileMode.Create, FileAccess.Write))
            using (var stamper = new PdfStamper(reader, fileStream))
            {
                // Definir a fonte e o tamanho
                var baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                var size = 12;

                // Processar cada página
                for (int i = 0; i < pdfDocument.PageCount; i++)
                {
                    // Renderizar a página como uma imagem
                    using (var pageImage = pdfDocument.Render(i, 600, 600, PdfRenderFlags.CorrectFromDpi))
                    {
                        var pageImagePath = $"page_{i}.png";
                        pageImage.Save(pageImagePath, System.Drawing.Imaging.ImageFormat.Png);

                        // Realizar OCR na imagem
                        using (var ocrEngine = new TesseractEngine(tessdataPath, language, EngineMode.Default))
                        {
                            using (var img = Pix.LoadFromFile(pageImagePath))
                            {
                                using (var ocrPage = ocrEngine.Process(img))
                                {
                                    // Adicionar o texto OCR ao PDF como uma camada transparente
                                    var canvas = stamper.GetOverContent(i + 1);
                                    canvas.BeginText();
                                    canvas.SetColorFill(new BaseColor(0, 0, 0, 0));
                                    canvas.SetFontAndSize(baseFont, size);
                                    var iterator = ocrPage.GetIterator();
                                    do
                                    {
                                        if (iterator.TryGetBoundingBox(PageIteratorLevel.Word, out var rect))
                                        {
                                            // Converter as coordenadas do Tesseract para as coordenadas do iTextSharp
                                            float x = rect.X1 * 595 / img.Width;  // 595 é a largura de uma página A4 em pontos
                                            float y = (img.Height - rect.Y1) * 842 / img.Height; // 842 é a altura de uma página A4 em pontos
                                            
                                            canvas.ShowTextAligned(Element.ALIGN_LEFT, iterator.GetText(PageIteratorLevel.Word), x, y, 0);
                                        }
                                    }
                                    while (iterator.Next(PageIteratorLevel.Word));
                                    canvas.EndText();
                                }
                            }
                        }

                        // Remover a imagem temporária
                        File.Delete(pageImagePath);
                    }
                }
            }
        }
    }
}
