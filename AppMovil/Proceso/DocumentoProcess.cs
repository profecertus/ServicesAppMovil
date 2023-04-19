using AppMovil.DAO;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Newtonsoft.Json;
using System.Xml;
using log4net.Config;
using log4net.Core;
using log4net;
using System.Reflection;
using Kevsoft.PDFtk;
using iTextSharp.text.pdf.parser;


namespace AppMovil.Proceso
{
    public class DocumentoProcess
    {
        private static ILog getLog()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("web.config"));
            ILog _logger = LogManager.GetLogger(typeof(LoggerManager));
            return _logger;
        }

        public async Task<byte[]> getPDFAsync(int idDocumento)
        {
            DocumentoDAO documentoDAO = new DocumentoDAO();
            AppMovil.DAO.DocumentoDAO.getLog().Info($"getPDFAsync - LLEGUE 0 = {idDocumento}");
            urlDocument rpta = documentoDAO.getUrlDocument(idDocumento);
            AppMovil.DAO.DocumentoDAO.getLog().Info($"getPDFAsync - LLEGUE 1");

            using var client = new HttpClient();
            AppMovil.DAO.DocumentoDAO.getLog().Info($"getPDFAsync - LLEGUE 2");
            AppMovil.DAO.DocumentoDAO.getLog().Info($"getPDFAsync URL - {rpta.url}");
            try
            {
                // Add an Accept header for JSON format.
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    //RequestUri = new Uri("https://esun.sunfruits.com.pe:7443/Mod_App/Wfo_GenerateBoleta?cPeriodo=202301&cSemana=02&nDni=47344883&vcPlanilla=OBR"),
                    RequestUri = new Uri(rpta.url),
                    //Content = new StringContent("some json", Encoding.UTF8, ContentType.Json),
                };
                var response = await client.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                byte[] bytesPDF = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                var pdftk = new PDFtk();
                var result = await pdftk.StampAsync(
                    bytesPDF,
                    await CrearPDFAsync(rpta, bytesPDF)
                );
                return result.Result;

            }catch(Exception e)
            {
                AppMovil.DAO.DocumentoDAO.getLog().Error(e.Message);
            }
            return null;
        }


        public static int pdfText(byte[] path)
        {
            PdfReader reader = new PdfReader(path);

            string text = PdfTextExtractor.GetTextFromPage(reader, 1);
            //Console.WriteLine(text);
            reader.Close();

            return text.Length;
        }

        public static async Task<byte[]> CrearPDFAsync(urlDocument rpta, byte[] bytesPDF)
        {
            int fontSize = 0, PageSize = 0;
            /*Verificar el tamanno de la hoja de bytesPDF*/
            iTextSharp.text.Document doc = new iTextSharp.text.Document();
            PdfReader rea = new PdfReader(bytesPDF);
            if (rea.GetPageSize(1).Width < 250)
            {
                PageSize = 212;
                fontSize = 8;
            }
            else
            {
                PageSize = 1300;
                fontSize = 18;
            }
            
            //var pgSize = new iTextSharp.text.Rectangle(212, 928);
            var pgSize = new iTextSharp.text.Rectangle(PageSize, 928);
            iTextSharp.text.Document document = new iTextSharp.text.Document(pgSize, 20f, 20f, 20f, 20f);            
            using (MemoryStream memoryStream = new MemoryStream())
            {
                PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                document.Open();
               
                PdfPTable tbfooter = new PdfPTable(1);
                tbfooter.HorizontalAlignment = Element.ALIGN_LEFT;
                tbfooter.TotalWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;
                tbfooter.DefaultCell.Border = 0;
                var _cell2 = new PdfPCell(new Paragraph(new Chunk($"Generado el : {rpta.fechaRecepcion.ToString("dd/MM/yyyy")} a las {rpta.fechaRecepcion.ToString("HH:mm:ss")} ", 
                    FontFactory.GetFont("Verdana", fontSize, iTextSharp.text.Font.NORMAL, iTextSharp.text.BaseColor.BLACK))));
                _cell2.HorizontalAlignment = Element.ALIGN_LEFT;
                _cell2.Border = 0;
                tbfooter.AddCell(_cell2);                
                tbfooter.WriteSelectedRows(0, -1, document.LeftMargin, writer.PageSize.GetBottom( document.BottomMargin + 30), writer.DirectContent);
                //*****************************
                document.Close();
                document.Dispose();
                byte[] bytes = memoryStream.ToArray();
                memoryStream.Close();
                memoryStream.Dispose();
                return bytes;
            }
        }


        public byte[] getXMLorFile(int idDocumento)
        {
            byte[] respuesta;
            DocumentoDAO documentoDAO = new DocumentoDAO();
            var rpta = documentoDAO.getDocumentoXML(idDocumento);

            try {
                if (rpta.contenido != null)
                {
                    BoletaXML bolXML = documentoDAO.getAllXML(idDocumento);

                    /**CABECERA**/
                    XmlDocument doc = new XmlDocument();
                    string json = string.Empty;
                    Cabecera cabecera = new Cabecera();
                    Empresa emp = new Empresa();
                    Nuevos nuevos = new Nuevos();
                    Detas detas = new Detas();
                    Horas horas = new Horas();
                    Tiempo tiempo = new Tiempo();

                    if (!string.IsNullOrEmpty(bolXML.cabe))
                    {
                        doc.LoadXml(bolXML.cabe);
                        json = JsonConvert.SerializeObject(doc);
                        //Obtengo la cabecera
                        cabecera = JsonConvert.DeserializeObject<Cabecera>(json);
                    }

                    /**EMPRESA**/
                    if (!string.IsNullOrEmpty(bolXML.empr))
                    {
                        doc.LoadXml(bolXML.empr);
                        json = JsonConvert.SerializeObject(doc);
                        //Obtengo la empresa
                        emp = JsonConvert.DeserializeObject<Empresa>(json);
                    }

                    /**NUEVO**/
                    if (!string.IsNullOrEmpty(bolXML.nuev))
                    {
                        doc.LoadXml($"<NUEVOS>{bolXML.nuev}<NUEV></NUEV></NUEVOS>");
                        json = JsonConvert.SerializeObject(doc);
                        //Obtengo NUEV
                        nuevos = JsonConvert.DeserializeObject<Nuevos>(json);
                        nuevos.NUEVOS.NUEV.Remove(nuevos.NUEVOS.NUEV[nuevos.NUEVOS.NUEV.Count - 1]);
                    }

                    /**DETA**/
                    if (!string.IsNullOrEmpty(bolXML.deta))
                    {
                        doc.LoadXml($"<DETAS>{bolXML.deta}<DETA></DETA></DETAS>");
                        json = JsonConvert.SerializeObject(doc);
                        //Obtengo NUEV
                        detas = JsonConvert.DeserializeObject<Detas>(json);
                        detas.DETAS.DETA.Remove(detas.DETAS.DETA[detas.DETAS.DETA.Count - 1]);
                    }

                    /**HORA**/
                    if (!string.IsNullOrEmpty(bolXML.hora))
                    {
                        doc.LoadXml($"<HORAS>{bolXML.hora}<HORA></HORA></HORAS>");
                        json = JsonConvert.SerializeObject(doc);
                        //Obtengo NUEV
                        horas = JsonConvert.DeserializeObject<Horas>(json);
                        horas.HORAS.HORA.Remove(horas.HORAS.HORA[horas.HORAS.HORA.Count - 1]);
                    }

                    /**TIEM**/
                    if (!string.IsNullOrEmpty(bolXML.tiem))
                    {
                        doc.LoadXml($"<TIEMPO>{bolXML.tiem}<TIEM></TIEM></TIEMPO>");
                        json = JsonConvert.SerializeObject(doc);
                        //Obtengo TIEM
                        tiempo = JsonConvert.DeserializeObject<Tiempo>(json);
                    }

                    byte[] pdf = CrearPDF(emp, cabecera, detas, horas, bolXML.fechaRecepcion, idDocumento);

                    return pdf;
                }
            }catch(Exception e)
            {
                getLog().Info("Error al momento de transformar el XML " + e.Message);
            }
            /*Si ya existe el archivo creado*/
            return null;
        }

        public byte[] CrearPDF(Empresa e, Cabecera c, Detas d, Horas h, DateTime fecha, int idDocumento)
        {
            var pgSize = new iTextSharp.text.Rectangle(212, 928);
            Document document = new Document(pgSize, 20f, 20f, 20f, 20f);
            try
            {
                if (e.EMPR == null || c.CABE == null || d.DETAS.DETA == null || h.HORAS.HORA == null)
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        DocumentoDAO documentoDAO = new DocumentoDAO();
                        documentoDAO.marcarError(idDocumento);


                        PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                        document.Open();

                        iTextSharp.text.Paragraph p1 = new iTextSharp.text.Paragraph("ERROR AL MOMENTO\n\r DE GENERAR SU BOLETA");
                        p1.Alignment = Element.ALIGN_CENTER;
                        p1.Font.Size = 8;
                        p1.Font.SetStyle(1);
                        p1.Font.Color = BaseColor.RED;

                        document.Add(p1);
                        document.Close();
                        byte[] bytes = memoryStream.ToArray();
                        memoryStream.Close();
                        return bytes;
                    }
                else
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                        document.Open();

                        iTextSharp.text.Paragraph lineSeparator = new iTextSharp.text.Paragraph(new Chunk(new iTextSharp.text.pdf.draw.LineSeparator(0.0F, 100.0F, BaseColor.BLACK, Element.ALIGN_LEFT, 1)));
                        // Set gap between line paragraphs.
                        lineSeparator.SetLeading(0.5F, 0.5F);

                        var logo = iTextSharp.text.Image.GetInstance("C://fonts/logo.png");
                        //logo.SetAbsolutePosition(0,0);
                        logo.ScaleAbsoluteHeight(40);
                        logo.ScaleAbsoluteWidth(70);
                        logo.Alignment = Element.ALIGN_CENTER;
                        document.Add(logo);


                        iTextSharp.text.Paragraph p = new iTextSharp.text.Paragraph(e.EMPR.cRazonSocialEmpr);
                        p.Alignment = Element.ALIGN_CENTER;
                        p.Font.Size = 6;

                        document.Add(p);

                        iTextSharp.text.Paragraph p1 = new iTextSharp.text.Paragraph(e.EMPR.cDirecEmpr);
                        p1.Alignment = Element.ALIGN_CENTER;
                        p1.Font.Size = 6;
                        document.Add(p1);

                        iTextSharp.text.Paragraph p2 = new iTextSharp.text.Paragraph(e.EMPR.cRuc);
                        p2.Alignment = Element.ALIGN_CENTER;
                        p2.Font.Size = 6;
                        document.Add(p2);

                        iTextSharp.text.Paragraph p3 = new iTextSharp.text.Paragraph("BOLETA DE REMUNERACIONES D.S. 001- 98");
                        p3.Alignment = Element.ALIGN_CENTER;
                        p3.Font.SetStyle(1);
                        p3.Font.Size = 6;
                        document.Add(p3);

                        iTextSharp.text.Paragraph p4 = new iTextSharp.text.Paragraph($"SEMANA: {c.CABE.SEMANA}");
                        p4.Alignment = Element.ALIGN_CENTER;
                        p4.Font.SetStyle(1);
                        p4.Font.Size = 6;
                        document.Add(p4);

                        iTextSharp.text.Paragraph p5 = new iTextSharp.text.Paragraph($"PERIODO DEL : {c.CABE.FECHA_INI.ToString("dd/MM/yyyy")}                    HASTA: {c.CABE.FECHA_FIN.ToString("dd/MM/yyyy")}");
                        p5.Alignment = Element.ALIGN_CENTER;
                        p5.Font.Size = 6;
                        document.Add(p5);

                        document.Add(lineSeparator);

                        iTextSharp.text.Paragraph p6 = new iTextSharp.text.Paragraph($"DNI : {c.CABE.NRODOCUMENTO}                                    CODIGO: {c.CABE.CODIGO_CONTROL}");
                        p6.Alignment = Element.ALIGN_LEFT;
                        p6.Font.Size = 6;
                        document.Add(p6);

                        iTextSharp.text.Paragraph p7 = new iTextSharp.text.Paragraph($"NOMBRES : {c.CABE.APE_NOMB}");
                        p7.Alignment = Element.ALIGN_LEFT;
                        p7.Font.Size = 6;
                        document.Add(p7);

                        document.Add(lineSeparator);

                        iTextSharp.text.Paragraph p8 = new iTextSharp.text.Paragraph($"CARGO : {c.CABE.CARGO}");
                        p8.Alignment = Element.ALIGN_LEFT;
                        p8.Font.Size = 6;
                        document.Add(p8);

                        iTextSharp.text.Paragraph p9 = new iTextSharp.text.Paragraph($"F. INGRESO : {c.CABE.FECHA_INGRESO.ToString("dd/MM/yyyy")}     CATEGORIA: {c.CABE.TIPOTRABAJADOR}");
                        p9.Alignment = Element.ALIGN_LEFT;
                        p9.Font.Size = 6;
                        document.Add(p9);

                        if (!string.IsNullOrEmpty(c.CABE.AFP))
                        {
                            iTextSharp.text.Paragraph p10 = new iTextSharp.text.Paragraph($"AFP : {c.CABE.AFP.Trim()}     CUSP: {c.CABE.AUTOGENERADOAFP.Trim()}");
                            p10.Alignment = Element.ALIGN_LEFT;
                            p10.Font.Size = 6;
                            document.Add(p10);
                        }


                        iTextSharp.text.Paragraph p11 = new iTextSharp.text.Paragraph(" ");
                        p11.Alignment = Element.ALIGN_LEFT;
                        p11.Font.Size = 6;
                        document.Add(p11);

                        PdfPTable table0 = new PdfPTable(2);
                        table0.WidthPercentage = 100;

                        Font fuenteCab = new Font();
                        fuenteCab.SetStyle(1);
                        fuenteCab.Size = 6;

                        PdfPCell cell = new PdfPCell(new Phrase("INGRESOS", fuenteCab));
                        cell.Colspan = 2;
                        table0.AddCell(cell);

                        Font fuente = new Font();
                        fuente.Size = 6;
                        decimal totalIn = 0;
                        getLog().Info("TOTAL INGRESOS = " + totalIn);
                        foreach (var det in d.DETAS.DETA)
                        {
                            if (det.IDTIPOCONCEPTO.Equals("IN"))
                            {
                                totalIn = totalIn + decimal.Parse(det.CALCULO);
                                getLog().Info($"CALCULO = {det.CALCULO}");
                                getLog().Info($"TOTAL CALCULO = {decimal.Parse(det.CALCULO)}");
                                getLog().Info($"TOTAL INGRESOS = {totalIn}");
                                PdfPCell cellMontoIng = new PdfPCell(new Phrase(det.CALCULO, fuente));
                                //cellMontoIng.Colspan = 2;
                                cellMontoIng.HorizontalAlignment = Element.ALIGN_RIGHT;
                                cellMontoIng.BorderWidthBottom = 0;
                                cellMontoIng.BorderWidthLeft = 0;
                                cellMontoIng.BorderWidthTop = 0;
                                PdfPCell cellDesc = new PdfPCell(new Phrase(det.DESCR_CORTA, fuente));
                                cellDesc.BorderWidthBottom = 0;
                                cellDesc.BorderWidthRight = 0;
                                cellDesc.BorderWidthTop = 0;
                                table0.AddCell(cellDesc);
                                table0.AddCell(cellMontoIng);
                            }
                        }
                        PdfPCell TotIng = new PdfPCell(new Phrase("TOTAL INGRESOS", fuenteCab));
                        TotIng.BorderWidthRight = 0;
                        table0.AddCell(TotIng);

                        PdfPCell TotIng0 = new PdfPCell(new Phrase($"S/. {(totalIn / 100).ToString().Replace(',', '.')}", fuenteCab));
                        TotIng0.BorderWidthLeft = 0;
                        TotIng0.HorizontalAlignment = Element.ALIGN_RIGHT;
                        table0.AddCell(TotIng0);

                        document.Add(table0);

                        document.Add(new iTextSharp.text.Paragraph("\n"));

                        PdfPTable table1 = new PdfPTable(2);
                        table1.WidthPercentage = 100;

                        PdfPCell cell2 = new PdfPCell(new Phrase("DESCUENTOS", fuenteCab));
                        cell2.Colspan = 2;
                        table1.AddCell(cell2);
                        decimal totalDes = 0;
                        foreach (var det in d.DETAS.DETA)
                        {
                            if (det.IDTIPOCONCEPTO.Equals("DE"))
                            {
                                totalDes = totalDes + decimal.Parse(det.CALCULO);
                                PdfPCell cellMontoIng = new PdfPCell(new Phrase(det.CALCULO, fuente));
                                //cellMontoIng.Colspan = 2;
                                cellMontoIng.HorizontalAlignment = Element.ALIGN_RIGHT;
                                cellMontoIng.BorderWidthBottom = 0;
                                cellMontoIng.BorderWidthLeft = 0;
                                cellMontoIng.BorderWidthTop = 0;
                                PdfPCell cellDesc = new PdfPCell(new Phrase(det.DESCR_CORTA, fuente));
                                cellDesc.BorderWidthBottom = 0;
                                cellDesc.BorderWidthRight = 0;
                                cellDesc.BorderWidthTop = 0;
                                table1.AddCell(cellDesc);
                                table1.AddCell(cellMontoIng);
                            }
                        }

                        PdfPCell TotDes = new PdfPCell(new Phrase("TOTAL DESCUENTOS", fuenteCab));
                        TotDes.BorderWidthRight = 0;
                        table1.AddCell(TotDes);
                        PdfPCell TotDes0 = new PdfPCell(new Phrase($"S/. {(totalDes / 100).ToString().Replace(',', '.')}", fuenteCab));
                        TotDes0.BorderWidthLeft = 0;
                        TotDes0.HorizontalAlignment = Element.ALIGN_RIGHT;
                        table1.AddCell(TotDes0);



                        document.Add(table1);

                        document.Add(new iTextSharp.text.Paragraph("\n"));

                        PdfPTable table2 = new PdfPTable(2);
                        table2.WidthPercentage = 100;

                        PdfPCell cell23 = new PdfPCell(new Phrase("APORTES DEL EMPLEADOR", fuenteCab));
                        cell23.Colspan = 2;
                        table2.AddCell(cell23);
                        decimal totalAE = 0;
                        foreach (var det in d.DETAS.DETA)
                        {
                            if (det.IDTIPOCONCEPTO.Equals("AE"))
                            {
                                totalAE = totalAE + decimal.Parse(det.CALCULO);
                                PdfPCell cellMontoIng = new PdfPCell(new Phrase(det.CALCULO, fuente));
                                //cellMontoIng.Colspan = 2;
                                cellMontoIng.HorizontalAlignment = Element.ALIGN_RIGHT;
                                cellMontoIng.BorderWidthBottom = 0;
                                cellMontoIng.BorderWidthLeft = 0;
                                cellMontoIng.BorderWidthTop = 0;
                                PdfPCell cellDesc = new PdfPCell(new Phrase(det.DESCR_CORTA, fuente));
                                cellDesc.BorderWidthBottom = 0;
                                cellDesc.BorderWidthRight = 0;
                                cellDesc.BorderWidthTop = 0;
                                table2.AddCell(cellDesc);
                                table2.AddCell(cellMontoIng);
                            }
                        }

                        PdfPCell TotAE = new PdfPCell(new Phrase("TOTAL APORTES DEL EMPLEADO", fuenteCab));
                        TotAE.BorderWidthRight = 0;
                        table2.AddCell(TotAE);
                        PdfPCell TotAE0 = new PdfPCell(new Phrase($"S/. {(totalDes / 100).ToString().Replace(',', '.')}", fuenteCab));
                        TotAE0.BorderWidthLeft = 0;
                        TotAE0.HorizontalAlignment = Element.ALIGN_RIGHT;
                        table2.AddCell(TotAE0);

                        document.Add(table2);

                        document.Add(new iTextSharp.text.Paragraph("\n"));

                        Font fuenteRes = new Font();
                        fuenteRes.SetStyle(1);
                        fuenteRes.Size = 4;

                        Font fuenteResDet = new Font();
                        fuenteResDet.Size = 4;

                        PdfPTable table3 = new PdfPTable(7);
                        table3.WidthPercentage = 100;

                        PdfPCell cell24 = new PdfPCell(new Phrase("DIA", fuenteRes));
                        cell24.BorderWidthBottom = 0;
                        cell24.BorderWidthRight = 0;
                        table3.AddCell(cell24);
                        cell24 = new PdfPCell(new Phrase("FECHA", fuenteRes));
                        cell24.BorderWidthBottom = 0;
                        cell24.BorderWidthRight = 0;
                        cell24.BorderWidthLeft = 0; ;
                        table3.AddCell(cell24);
                        cell24 = new PdfPCell(new Phrase("HORAS", fuenteRes));
                        cell24.BorderWidthBottom = 0;
                        cell24.BorderWidthRight = 0;
                        cell24.BorderWidthLeft = 0;
                        table3.AddCell(cell24);
                        cell24 = new PdfPCell(new Phrase("REND", fuenteRes));
                        cell24.BorderWidthBottom = 0;
                        cell24.BorderWidthLeft = 0;
                        cell24.BorderWidthRight = 0;
                        table3.AddCell(cell24);
                        PdfPCell cellVacio = new PdfPCell(new Phrase("", fuente));
                        cellVacio.BorderWidthBottom = 0;
                        cellVacio.BorderWidthTop = 0;
                        table3.AddCell(cellVacio);
                        cell24 = new PdfPCell(new Phrase("RESUMEN", fuenteRes));
                        cell24.Colspan = 2;
                        table3.AddCell(cell24);

                        int contador = 0;

                        foreach (var det in h.HORAS.HORA)
                        {
                            contador++;
                            PdfPCell cellIzq = new PdfPCell(new Phrase(det.DIA, fuenteResDet));
                            cellIzq.BorderWidthBottom = 0;
                            cellIzq.BorderWidthTop = 0;
                            cellIzq.BorderWidthRight = 0;
                            table3.AddCell(cellIzq);
                            PdfPCell cell0 = new PdfPCell(new Phrase(det.FECHA.ToString("dd/MM/yyyy"), fuenteResDet));
                            cell0.BorderWidth = 0;
                            table3.AddCell(cell0);
                            PdfPCell cellA = new PdfPCell(new Phrase(det.HORAS, fuenteResDet));
                            cellA.BorderWidth = 0;
                            table3.AddCell(cellA);
                            PdfPCell cellB = new PdfPCell(new Phrase(det.RENDIMIENTO, fuenteResDet));
                            cellB.BorderWidth = 0;
                            table3.AddCell(cellB);
                            table3.AddCell(cellVacio);
                            table3.AddCell(new PdfPCell(new Phrase("", fuenteResDet)));
                            table3.AddCell(new PdfPCell(new Phrase("", fuenteResDet)));
                        }

                        int othercount = 0;
                        foreach (var celda in table3.GetRow(contador).GetCells())
                        {
                            othercount++;
                            if (othercount != 5)
                                celda.BorderWidthBottom = 1;
                        }


                        document.Add(table3);

                        document.Add(new iTextSharp.text.Paragraph("\n"));

                        PdfPTable table4 = new PdfPTable(1);
                        table4.WidthPercentage = 100;

                        PdfPCell TotNP = new PdfPCell(new Phrase("NETO A PAGAR", fuenteCab));
                        TotNP.BorderWidthRight = 0;
                        table4.AddCell(TotNP);
                        PdfPCell TotNP0 = new PdfPCell(new Phrase($"S/. {((totalIn - totalDes) / 100).ToString().Replace(',', '.')}", fuenteCab));
                        TotNP0.BorderWidthLeft = 0;
                        TotNP0.HorizontalAlignment = Element.ALIGN_RIGHT;
                        table4.AddCell(TotNP0);


                        document.Add(table4);
                        fuenteRes.Size = 4;
                        document.Add(new iTextSharp.text.Paragraph(c.CABE.BANCO, fuenteRes));
                        document.Add(new iTextSharp.text.Paragraph($"CTA AHORRO: {c.CABE.CUENTA_BANCO}", fuenteRes));
                        document.Add(new iTextSharp.text.Paragraph("\n"));
                        document.Add(new iTextSharp.text.Paragraph("\n"));

                        iTextSharp.text.Paragraph pFecha = new iTextSharp.text.Paragraph($"{c.CABE.APE_NOMB}");
                        pFecha.Alignment = Element.ALIGN_CENTER;
                        pFecha.Font.Size = 6;
                        document.Add(pFecha);

                        iTextSharp.text.Paragraph pFechaRec = new iTextSharp.text.Paragraph($"FECHA REC. {fecha}");
                        pFechaRec.Alignment = Element.ALIGN_CENTER;
                        pFechaRec.Font.Size = 6;
                        document.Add(pFechaRec);

                        iTextSharp.text.Paragraph ptrab = new iTextSharp.text.Paragraph("TRABAJADOR  ");
                        ptrab.Alignment = Element.ALIGN_CENTER;
                        ptrab.Font.Size = 6;
                        document.Add(ptrab);


                        var firma = iTextSharp.text.Image.GetInstance("C://fonts/firma.png");
                        firma.SetAbsolutePosition(10, 325);
                        firma.ScaleAbsoluteHeight(50);
                        firma.ScaleAbsoluteWidth(50);
                        firma.Alignment = Element.ALIGN_LEFT;
                        document.Add(firma);


                        document.Close();
                        byte[] bytes = memoryStream.ToArray();
                        memoryStream.Close();
                        return bytes;


                    }
            }catch(Exception ex)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    DocumentoDAO documentoDAO = new DocumentoDAO();
                    documentoDAO.marcarError(idDocumento);


                    PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                    document.Open();

                    iTextSharp.text.Paragraph p1 = new iTextSharp.text.Paragraph("ERROR AL MOMENTO\n\r DE OBTENER LOS DATOS\n\r DE SU BOLETA");
                    p1.Alignment = Element.ALIGN_CENTER;
                    p1.Font.Size = 8;
                    p1.Font.SetStyle(1);
                    p1.Font.Color = BaseColor.RED;

                    document.Add(p1);
                    document.Close();
                    byte[] bytes = memoryStream.ToArray();
                    memoryStream.Close();
                    return bytes;
                }
            }
        }
    }



    public class Evaluacion
    {
        public EVAL EVAL { get; set; }
    }

    public class EVAL
    {
        public string bReImpr { get; set; }
        public string dFecha { get; set; }
    }

    public class Horas
    {
        public Hora HORAS { get; set; }
    }

    public class Tiempo
    {
        public Tiemp TIEMP { get; set; }
    }

    public class Tiemp
    {
        public List<Tiem> TIEMP{ get; set; }
    }

    public class Tiem
    {
        public TIEM TIEM { get; set; }
    }

    public class TIEM
    {
        public string HORAS_NORMALES { get; set; }
        public string HORAS_EXTRAS { get; set; }
        public string HORAS_EXTRAS2 { get; set; }
        public string HORAS_DOBLES { get; set; }
        public string HORAS_NOCTURNAS { get; set; }
        public string HORAS_EXTNOCTU { get; set; }
        public string HORAS_EXTNOCTU2 { get; set; }
        public string HORAS_DOBNOCTU { get; set; }
        public string HORAS_DOBEXTNOCTU { get; set; }
        public string DIAS_TRABAJADOS { get; set; }
        public string DIAS_RENDIMIENTO { get; set; }
        public string FALTAS { get; set; }
        public string Neto { get; set; }
    }
    public class Hora
    {
        public List<DataHora> HORA { get; set; }
    }

    public class DataHora
    {
        public string IDEMPRESA { get; set; }
        public string IDCODIGOGENERAL { get; set; }
        public string DIA { get; set; }
        public DateTime FECHA { get; set; }
        public string HORAS { get; set; }
        public string RENDIMIENTO { get; set; }
    }

    public class Detas
    {
        public Deta DETAS { get; set; }
    }

    public class Deta
    {
        public List<DataDeta> DETA { get; set; }
    }

    public class DataDeta
    {
        public string IDEMPRESA { get; set; }
        public string IDCODIGOGENERAL { get; set; }
        public string IDCONCEPTO { get; set; }
        public string DESCRIPCION { get; set; }
        public string DESCR_CORTA { get; set; }
        public string CALCULO { get; set; }
        public string IDTIPOCONCEPTO { get; set; }
        public string TIPOCONCEPTO { get; set; }
        public string ORDEN { get; set; }
    }

    public class Nuevos
    {
        public Nuev NUEVOS { get; set; }
    }

    public class Nuev
    {
        public List<DetaNuev> NUEV { get; set; }
    }

    public class DetaNuev
    {
        public string Desc_J { get; set; }
        public string valor_J { get; set; }
        public string Desc_D { get; set; }
        public string valor_D { get; set; }
        public string norden { get; set; }
    }
    public class Empresa
    {
        public EMPR EMPR { get; set; }
    }

    public class EMPR
    {
        public string nIdEmpresa { get; set; }
        public string IDEMPRESA { get; set; }
        public string cRuc { get; set; }
        public string cRazonSocialEmpr { get; set; }
        public string cDirecEmpr { get; set; }
        public string IDPLANILLA { get; set; }
    }

    public class Cabecera
    {
        public CABE CABE { get; set; }
    }

    public class CABE
    {
        public string IDEMPRESA { get; set; }
        public string IDCODIGOGENERAL { get; set; }
        public string CERRADO { get; set; }
        public string PERIODO { get; set; }
        public string SEMANA { get; set; }
        public DateTime FECHA_INI { get; set; }
        public DateTime FECHA_FIN { get; set; }
        public string NRODOCUMENTO { get; set; }
        public string APE_NOMB { get; set; }
        public string CODIGO_CONTROL { get; set; }
        public string IDCARGO { get; set; }
        public string CARGO { get; set; }
        public DateTime FECHA_INGRESO { get; set; }
        public string IDTIPOTRABAJADOR { get; set; }
        public string TIPOTRABAJADOR { get; set; }
        public string IDAFP { get; set; }
        public string AFP { get; set; }
        public string AUTOGENERADOAFP { get; set; }
        public string CUENTA_BANCO { get; set; }
        public string IDBANCO { get; set; }
        public string BANCO { get; set; }
        public object AUTOGENERADOIPSS { get; set; }
        public string BASICO { get; set; }
        public string DIRECCION { get; set; }
    }
}
