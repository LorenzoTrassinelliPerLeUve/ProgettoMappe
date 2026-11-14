using GeoJSON.Net.Contrib.EntityFramework;
using GeoJSON.Net.Geometry;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity.Spatial;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Web;
using GeoJSON.Net;

using System.Threading;
using System.Globalization;
using System.Data.Entity.Validation;
using _4GrapesAPI.Entity;
using System.Linq.Expressions;
using System.Web.Caching;
using Microsoft.AspNet.Identity;
using OfficeOpenXml;
using System.Data;
using Microsoft.SqlServer.Types;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Microsoft.Azure.KeyVault.Core;
using System.Web.Http.Results;
using System.Web.Routing;
using static _4GrapesAPI.Utils;
using System.Buffers.Text;

namespace _4GrapesAPI
{
    public static class Utils
    {
        private static List<UnitaDiMisura> _UnitaDiMisura;

        static Cache CacheImpostazioni = new Cache();

        public static Impostazioni Impostazioni
        {
            get
            {
                var impostazioni = (Impostazioni)CacheImpostazioni.Get("Impostazioni");
                
                if (impostazioni == null)
                {
                    impostazioni = RicaricaImpostazioni();
                }

                return impostazioni;
            }
        }

        public static Impostazioni RicaricaImpostazioni()
        {
            using (var db = new DB())
            {                
                var impostazioni = db.Impostazioni.Find(0);
                CacheImpostazioni["Impostazioni"] = impostazioni;
                return impostazioni;
            }

        }

        public static int ID(this IPrincipal User)
        {
            return int.Parse(((ClaimsIdentity)User.Identity).FindFirstValue(ClaimTypes.Sid));
        }       

        public static string DescrizioneInLingua(this AvversitaCarenza AvversitaCarenza, string lingua)
        {
            return ValoreInLingua(AvversitaCarenza, "AvversitaCarenza", lingua);
        }

        public static string DescrizioneInLingua(this DistribuzioneVite DistribuzioneVite, string lingua)
        {
            return ValoreInLingua(DistribuzioneVite, "DistribuzioneVite", lingua);
        }

        public static string DescrizioneInLingua(this DistribuzioneVigneto DistribuzioneVigneto, string lingua)
        {
            return ValoreInLingua(DistribuzioneVigneto, "DistribuzioneVigneto", lingua);
        }

        public static string DescrizioneInLingua (this BBCH BBCH, string lingua)
        {
            return ValoreInLingua(BBCH, "DescrizioneBBCH", lingua);
        }

        public static string DescrizioneInLingua(this EtaEpocaGenerazione EtaEpocaGenerazione, string lingua)
        {
            return ValoreInLingua(EtaEpocaGenerazione, "EtaEpocaGenerazione", lingua);
        }

        public static string DescrizioneInLingua(this BiodiversitaMicrobiomaSuolo entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this ColoreSuolo entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this CoperturaVegetaleSuolo entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this FertilitaComplessivaSuolo entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this PietrositaSuolo entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this ProfonditaSuolo entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this RistagnoIdricoSuolo entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this SpecieIndicatriciSuolo entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this StrutturaSuolo entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this TessituraSuolo entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this SpeciePresenti entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this TipoGestioneSuolo entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this MotivoLavorazione entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this MotivoCampione MotivoCampione, string lingua)
        {
            return ValoreInLingua(MotivoCampione, "MotivoCampione", lingua);
        }

        public static string DescrizioneInLingua(this OrigineInerbimento entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this UltimaOperazioneEseguita entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this DiversitaDiSpecie entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this ZollositaPercentuale entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this EfficaciaDellaLavorazione entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this MaturitaTecnologicaDellaBacca entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this MaturitaAromaticaDellaPolpa entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this MaturitaDellaBuccia entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this MaturitaDeiVinaccioli entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this ValutazioneStatoIdrico entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this Sensore entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this TipoOrganismoUtile entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this OperazioneEseguita entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this TipoFeromone entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this FaseDiSviluppo entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this MorfologiaGrappolo entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this MalDellEsca entita, string lingua)
        {
            return ValoreInLingua(entita, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this Organo Organo, string lingua)
        {
            return ValoreInLingua(Organo, "Organo", lingua);
        }

        public static string DescrizioneInLingua(this Sporulazione Sporulazione, string lingua)
        {
            return ValoreInLingua(Sporulazione, "Sporulazione", lingua);
        }

        public static string DescrizioneInLingua(this StatoVegetativo StatoVegetativo, string lingua)
        {
            return ValoreInLingua(StatoVegetativo, "StatoVegetativo", lingua);
        }

        public static string DescrizioneInLingua(this TipoCampione TipoCampione, string lingua)
        {
            return ValoreInLingua(TipoCampione, "TipoCampione", lingua);
        }

        public static string DescrizioneInLingua(this TipoRilievo TipoRilievo, string lingua)
        {
            //--- Just a Bogus on database
            return ValoreInLingua(TipoRilievo, "TipoRilevo", lingua);
        }

        public static string DescrizioneInLingua(this MisuraConsiderata MisuraConsiderata, string lingua)
        { 
            return ValoreInLingua(MisuraConsiderata, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this CategoriaRilievo CategoriaRilievo, string lingua)
        {
            return ValoreInLingua(CategoriaRilievo, "CategoriaRilievo", lingua);
        }

        public static string DescrizioneInLingua(this Conduzione Conduzione, string lingua)
        {
            return ValoreInLingua(Conduzione, "Conduzione", lingua);
        }

        public static string DescrizioneInLingua(this UnitaDiMisura UnitaDiMisura, string lingua)
        {
            return ValoreInLingua(UnitaDiMisura, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this Area Area, string lingua)
        {
            return ValoreInLingua(Area, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this SistemaMetrico SistemaMetrico, string lingua)
        {
            return ValoreInLingua(SistemaMetrico, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this Irrigazione Irrigazione, string lingua)
        {
            return ValoreInLingua(Irrigazione, "Irrigazione", lingua);
        }

        public static string DescrizioneInLingua(this Giacitura Giacitura, string lingua)
        {
            return ValoreInLingua(Giacitura, "Giacitura", lingua);
        }

        public static string DescrizioneInLingua(this FormaAllevamento FormaAllevamento, string lingua)
        {
            return ValoreInLingua(FormaAllevamento, "FormaAllevamento", lingua);
        }

        public static string DescrizioneInLingua(this GestioneChioma GestioneChioma, string lingua)
        {
            return ValoreInLingua(GestioneChioma, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this TransitoNelVigneto TransitoNelVigneto, string lingua)
        {
            return ValoreInLingua(TransitoNelVigneto, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this SpecificaRilievo SpecificaRilievo, string lingua)
        {
            return ValoreInLingua(SpecificaRilievo, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this AvversitaDaTrattare AvversitaDaTrattare, string lingua)
        {
            return ValoreInLingua(AvversitaDaTrattare, "Descrizione", lingua);
        }

        public static string DescrizioneInLingua(this Rischio Rischio, string lingua)
        {
            return ValoreInLingua(Rischio, "Descrizione", lingua);
        }

        public static string TipoRilievo_DescrizioneInLingua(this VistaGalleriaFotografica entita, string lingua)
        {
            return ValoreInLingua(entita, "TipoRilievo", lingua);
        }

        public static string DescrizioneEpocaInLingua(this VistaConcimazioneReale entita, string lingua)
        {
            return ValoreInLingua(entita, "DescrizioneEpoca", lingua);
        }

        public static string DescrizioneModalitaInLingua(this VistaConcimazioneReale entita, string lingua)
        {
            return ValoreInLingua(entita, "DescrizioneModalita", lingua);
        }

        public static string DescrizioneEpocaInLingua(this VistaConcimazioneRealePerTarget entita, string lingua)
        {
            return ValoreInLingua(entita, "DescrizioneEpoca", lingua);
        }

        public static string DescrizioneModalitaInLingua(this VistaConcimazioneRealePerTarget entita, string lingua)
        {
            return ValoreInLingua(entita, "DescrizioneModalita", lingua);
        }

        public static string ValoreInLingua(object obj, string field, string lingua)
        {
            string value = null;

            var props = obj.GetType().GetProperties();
            var prop = props.FirstOrDefault(x => x.Name == field + "_" + lingua);

            if (prop != null)
            {
                value = prop.GetValue(obj, null)?.ToString();                

                if (value != null)
                {
                    return value;
                }
            }
            
            prop = props.FirstOrDefault(x => x.Name == field);

            if (prop != null)
            {
                value = prop.GetValue(obj, null)?.ToString();

                if (value != null)
                {
                    return value;
                }
            }

            prop = props.FirstOrDefault(x => x.Name == field + "1");

            if (prop != null)
            {
                value = prop.GetValue(obj, null)?.ToString();

                if (value != null)
                {
                    return value;
                }
            }

            return "";

        }

        public static string MaxCaratteri(this string str, int n)
        {
            if (str.Length <= n)
            {
                return str;
            }

            return str.Substring(0, n);
        }

        public static Exception GetFullEntityException(this DbEntityValidationException ex)
        {
            var errors = new List<string>();
            foreach (var validationErrors in ex.EntityValidationErrors)
            {
                foreach (var validationError in validationErrors.ValidationErrors)
                {
                    errors.Add(string.Format("{0}:{1}", validationErrors.Entry.Entity.ToString(), validationError.ErrorMessage));
                }
            }

            return new Exception(string.Join(Environment.NewLine, errors), ex);
        }

        public static string ComputeHash(string input)
        {
            var algorithm = new SHA256CryptoServiceProvider();

            Byte[] inputBytes = Encoding.UTF8.GetBytes(input);

            Byte[] hashedBytes = algorithm.ComputeHash(inputBytes);

            return string.Concat(hashedBytes.Select(x => x.ToString("X2")));
        }       


        /// <summary>
        /// SEE  http://www.dotnet4techies.com/2014/09/reset-image-orientation-using-exif-in-c.html
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool IfPhotoRemoveExif(string filename)
        {

            try
            {
                if (!filename.EndsWith(".jpg"))
                {
                    return false;
                }

                if (!MimeMapping.GetMimeMapping(filename).StartsWith("image/"))
                    {
                        return false;
                    }

                    string file_tmp = filename + "__";

                    using (Image img = Image.FromFile(filename))
                    {
                        var orientationTag = img.GetPropertyItem(0x0112);
                        int orientationValue = orientationTag.Value[0];
                        var rotateFlipType = GetOrientationToFlipType(orientationValue);

                        if (rotateFlipType == RotateFlipType.RotateNoneFlipNone)
                        {
                            return true;
                        }

                        img.RotateFlip(rotateFlipType);
                        orientationTag.Value[0] = 1;
                        img.SetPropertyItem(orientationTag);

                        img.Save(file_tmp, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }

                    File.Delete(filename);
                    File.Move(file_tmp, filename);

                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }                        
        }

        public static MemoryStream IfPhotoRemoveExif(Stream stream, string filename)
        {

            try
            {
                if (!filename.EndsWith(".jpg"))
                {
                    return null;
                }

                /*if (!MimeMapping.GetMimeMapping(filename).StartsWith("image/"))
                {
                    return false;
                }*/

                string file_tmp = filename + "__";

                using (Image img = Image.FromStream(stream))
                {
                    var orientationTag = img.GetPropertyItem(0x0112);
                    int orientationValue = orientationTag.Value[0];
                    var rotateFlipType = GetOrientationToFlipType(orientationValue);

                    if (rotateFlipType == RotateFlipType.RotateNoneFlipNone)
                    {
                        return null;
                    }

                    img.RotateFlip(rotateFlipType);
                    orientationTag.Value[0] = 1;
                    img.SetPropertyItem(orientationTag);

                    var ms = new MemoryStream();
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    ms.Position = 0;
                    return ms;
                }               
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private static RotateFlipType GetOrientationToFlipType(int orientation)
        {
            switch (orientation)
            {
                case 1:
                    return RotateFlipType.RotateNoneFlipNone;
                case 2:
                    return RotateFlipType.RotateNoneFlipX;
                case 3:
                    return RotateFlipType.Rotate180FlipNone;
                case 4:
                    return RotateFlipType.Rotate180FlipX;
                case 5:
                    return RotateFlipType.Rotate90FlipX;
                case 6:
                    return RotateFlipType.Rotate90FlipNone;
                case 7:
                    return RotateFlipType.Rotate270FlipX;
                case 8:
                    return RotateFlipType.Rotate270FlipNone;
                default:
                    return RotateFlipType.RotateNoneFlipNone;
            }
        }

        public static bool IfBigPhotoCompres(string filename)
        {

            try
            {
                if (!filename.ToLower().EndsWith(".jpg"))
                {
                    return false;
                }

                string file_tmp = filename + "_TMP_";

                if (new FileInfo(filename).Length < 10 * 1024 * 1024)
                {
                    return false;
                }

                using (Image img = Image.FromFile(filename))
                {
                    try
                    {

                        var orientationTag = img.GetPropertyItem(0x0112);
                        int orientationValue = orientationTag.Value[0];
                        var rotateFlipType = GetOrientationToFlipType(orientationValue);

                        if (rotateFlipType != RotateFlipType.RotateNoneFlipNone)
                        {
                            img.RotateFlip(rotateFlipType);
                            orientationTag.Value[0] = 1;
                            img.SetPropertyItem(orientationTag);
                        }
                    }
                    catch (Exception ex)
                    {

                    }               


                    img.Save(file_tmp, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                File.Delete(filename);
                File.Move(file_tmp, filename);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }


        }

        public static bool IfPhotoRestoreExif(string filename)
        {

            try
            {
                if (!filename.EndsWith(".jpg"))
                {
                    return false;
                }

                if (!MimeMapping.GetMimeMapping(filename).StartsWith("image/"))
                {
                    return false;
                }

                string file_tmp = filename + "__";

                using (Image img = Image.FromFile(filename))
                {
                    var orientationTag = img.GetPropertyItem(0x0112);
                    //int orientationValue = orientationTag.Value[0];
                    //var rotateFlipType = GetOrientationToFlipType(orientationValue);

                    if (orientationTag.Value[0] == 1)
                    {
                        return true;
                    }                    

                    //img.RotateFlip(rotateFlipType);
                    orientationTag.Value[0] = 1;
                    img.SetPropertyItem(orientationTag);

                    img.Save(file_tmp, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                File.Delete(filename);
                File.Move(file_tmp, filename);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }


        }

        public static MemoryStream ResizeImage(Stream stream, int max)
        {
            int width, height;

            using (var image = Image.FromStream(stream))
            {

                //if (image.Width < width && image.Height < height)
                //{
                if (image.Width > image.Height)
                {
                    height = (int)(max * image.Height / (double)image.Width);
                    width = max;
                }
                else
                {
                    width = (int)(max * image.Width / (double)image.Height);
                    height = max;
                }
                //}                                      

                var destImage = new Bitmap(width, height);

                destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                using (var graphics = Graphics.FromImage(destImage))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    /*using (var wrapMode = new ImageAttributes())
                    {
                        wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                        graphics.DrawImage(image, new Rectangle(0, 0, width, height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode); 
                    }*/

                    graphics.DrawImage(image, new Rectangle(0, 0, width, height), new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);

                    var ms = new MemoryStream();
                    EncoderParameters myEncoderParameters = new EncoderParameters(1);
                    EncoderParameter myEncoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L);
                    myEncoderParameters.Param[0] = myEncoderParameter;

                    destImage.Save(ms, GetEncoder(ImageFormat.Jpeg), myEncoderParameters);
                    ms.Position = 0;
                    return ms;
                }
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public static string FormatWith(this string str, object par)
        {
            try
            {
                return string.Format(str, par);
            }
            catch (Exception ex)
            {
                return str;
            }
        }

        public static string FormatWith(this string str, params object[] par)
        {
            try
            {
                return string.Format(str, par);
            }
            catch (Exception ex)
            {
                return str;
            }
        }

        public static bool IsEmpty(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        public static bool IsNotEmpty(this string str)
        {
            return !string.IsNullOrWhiteSpace(str);
        }

        public static int? ToInt(this string str)
        {
            int num;
            if (int.TryParse(str, out num))
            {
                return num;
            }
            else
            {
                return null;
            }
        }

        public static decimal? ToDecimal(this string str)
        {
            decimal num;
            if (decimal.TryParse(str, out num))
            {
                return num;
            }
            else
            {
                return null;
            }
        }

        public static double? ToDouble(this string str)
        {
            double num;
            if (double.TryParse(str, out num))
            {
                return num;
            }
            else
            {
                return null;
            }
        }

        public static bool Uguale(this string str1, string str2)
        {
            if (string.IsNullOrWhiteSpace(str1))
            {
                return string.IsNullOrWhiteSpace(str2);
            }

            return str1.Trim() == str2.Trim();            
        }

        public static string ToStringMaps(this DbGeometry geometry)
        {            
            if (geometry == null)
            {
                return null;
            }
            
            return JsonConvert.SerializeObject(geometry.ToGeoJSONObject<GeoJSONObject>());          
        }

        public static DbGeometry ToDbGeometry(this string poligono)
        {
            if (poligono == null)
            {
                return null;
            }
            
            var geoJSON = JsonConvert.DeserializeObject<JsonGeometryType>(poligono);

            IGeometryObject geometry;
            if (geoJSON.type == JsonGeometryType.Polygon)
            {
                geometry = JsonConvert.DeserializeObject<Polygon>(poligono);
            }
            else if (geoJSON.type == JsonGeometryType.MultiPolygon)
            {
                geometry = JsonConvert.DeserializeObject<MultiPolygon>(poligono);
            }
            else
            {
                throw new Exception($"Tipo di poligono non supportato: {geoJSON.type}");
            }

            return geometry.ToDbGeometry();
        }

        public static decimal CalcolaArea(this DbGeometry geometry)
        {
            var sqlGeography = SqlGeography.STGeomFromWKB(new System.Data.SqlTypes.SqlBytes(geometry.AsBinary()), DbGeography.DefaultCoordinateSystemId);

            // just trying to not return almost the whole area of our beloved planet...
            return (decimal)Math.Min(sqlGeography.STArea().Value, sqlGeography.ReorientObject().STArea().Value);
        }

        public static string AddUM(this string str, UnitaDiMisuraBase um, Utente utente)
        {
            return "{0} ({1})".FormatWith(str, um.UnitaDiMisuraDesc(utente));
        }

        public static string UnitaDiMisuraDesc(this UnitaDiMisuraBase um_base, Utente utente)
        {
            var um = GetUnitaDiMisura(um_base, utente);

            return um.Descrizione;
        }

        static List<UnitaDiMisura> UnitaDiMisura
        {
            get
            {
                if (_UnitaDiMisura == null)
                {
                    using (var db = new DB())
                    {
                        _UnitaDiMisura = db.UnitaDiMisura.ToList();
                    }
                }

                return _UnitaDiMisura;
            }
        }

        public static UnitaDiMisura GetUnitaDiMisura(UnitaDiMisuraBase um_base, Utente utente)
        {
            var sistema_metrico = utente.SistemaMetrico ?? 1;
            var um = UnitaDiMisura.Find(x => x.IdUnitaDiMisuraBase == (int)um_base && x.SistemaMetrico == sistema_metrico);

            if (um == null && sistema_metrico != 1)
            {
                um = UnitaDiMisura.Find(x => x.IdUnitaDiMisuraBase == (int)um_base && x.SistemaMetrico == 1);                
            }

            return um;
        }

        public static TKey Moda<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return source.GroupBy(keySelector)
                       .OrderByDescending(x => x.Count())
                       .ThenByDescending(x => x.Key)
                       .Take(1)
                       .Select(x => x.Key)
                       .FirstOrDefault();
        }

        public static void SetTestiInLingua(this Utente utente)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(utente.Lingua);
        }

        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, string sortDirection)
        {
            if (sortDirection == "asc")
                return source.OrderBy(keySelector);
            else
                return source.OrderByDescending(keySelector);
        }

        public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, string sortDirection)
        {
            if (sortDirection == "asc")
                return source.ThenBy(keySelector);
            else
                return source.ThenByDescending(keySelector);
        }

        public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, string sortDirection)
        {
            if (sortDirection == "asc")
                return source.OrderBy(keySelector);
            else
                return source.OrderByDescending(keySelector);
        }

        public static IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, string sortDirection)
        {
            if (sortDirection == "asc")
                return source.ThenBy(keySelector);
            else
                return source.ThenByDescending(keySelector);
        }

        public static string ToHtml(this string str)
        {
            if (str == null)
            {
                return null;
            }

            return HttpUtility.HtmlEncode(str).Replace("\r\n", "<br>").Replace("\n", "<br>").Replace("\r", "<br>");
        }

        public static string EscapeHtmlAttr(this string text)
        {
            return text.Replace("'", "&#39;").Replace("\"", "&quot;");
        }

        /*public static string ToStringJS(this string str)
        {
            //return str.Replace("\"", "\\\"");
        }*/

        public static decimal RadiceCubica(decimal x)
        {            
            if (x < 0)
                return -(decimal)Math.Pow(-(double)x, 1d / 3d);
            else
                return (decimal)Math.Pow((double)x, 1d / 3d);            
        }

        public static bool EqualsIgnoreCase(this string source, string toCheck)
        {            
            if (source == null || toCheck == null)
            {
                return source == toCheck;
            }

            return source.Equals(toCheck, StringComparison.OrdinalIgnoreCase);
            //return source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) == 0;
        }
        public static bool ContainsIgnoreCase(this IEnumerable<string> source, string toCheck)
        {
            if (source == null) return false;

            return source.Contains(toCheck, StringComparer.OrdinalIgnoreCase);

            //return source.Any(x => x.EqualsIgnoreCase(toCheck));
        }

        public static DateTime StartOfYear(this DateTime date)
        {
            return new DateTime(date.Year, 1, 1);
        }

        public static DateTime EndOfYear(this DateTime date)
        {
            return new DateTime(date.Year + 1, 1, 1).AddTicks(-1);
        }

        public static DateTime StartOfDay(this DateTime date)
        {
            return date.Date;
        }

        public static DateTime EndOfDay(this DateTime date)
        {
            return date.Date.AddDays(1).AddTicks(-1);
        }

        public static string Join(this IEnumerable<string> list, string separator)
        {
            return string.Join(separator, list);
        }

        public static string Join<T>(this IEnumerable<T> list, string separator)
        {
            return string.Join(separator, list);
        }

        public static decimal MapValue(decimal value, decimal x1, decimal x2, decimal y1, decimal y2)
        {            
            if (x1 == x2) return (y1 + y2) / 2;
            if (x1 < x2 && value <= x1) return y1;
            if (x1 < x2 && value >= x2) return y2;
            if (x1 > x2 && value >= x1) return y1;
            if (x1 > x2 && value <= x2) return y2;

            return (value - x1) * (y2 - y1) / (x2 - x1) + y1;
        }

        //--- MAP DI 2 FUNZIONI LINEARI
        public static decimal MapValue(decimal value, decimal x1, decimal x2, decimal x3, decimal y1, decimal y2, decimal y3)
        {
            if (value <= x2)
            {
                return MapValue(value, x1, x2, y1, y2);
            }
            else
            {
                return MapValue(value, x2, x3, y2, y3);
            }
        }

        public static string GetColoreDaScala(decimal value, decimal low, decimal high)
        {
            if (value < low)
            {
                return "green";
            }
            else if (value < high)
            {
                return "orange";
            }
            else
            {
                return "red";
            }
        }

        public static T CloneEntity<T>(this T source) where T : class, new()
        {
            if (source == null)
            {
                return null;
            }

            var sourceProps = typeof(T).GetProperties().Where(x => x.CanRead).ToList();
            var destProps = typeof(T).GetProperties().Where(x => x.CanWrite).ToList();

            var dest = new T();

            foreach (var sourceProp in sourceProps)
            {
                var destProp = destProps.FirstOrDefault(x => x.Name == sourceProp.Name);
                if (destProp != null && (destProp.PropertyType.IsValueType || destProp.PropertyType.IsSealed))
                {
                    destProp.SetValue(dest, sourceProp.GetValue(source, null), null);
                }
            }

            return dest;
        }

        public static void InsertDataInSheet(this ExcelPackage package, DataTable table, string sheet_name, string datetime_format = "yyyy-MM-dd HH:mm:ss.fffffff")
        {
            // add a new worksheet to the empty workbook
            ExcelWorksheet writer = package.Workbook.Worksheets.Add(sheet_name);
            for (int j = 0; j < table.Columns.Count; j++)
            {
                writer.Cells[1, j + 1].Value = table.Columns[j].ColumnName;
            }

            for (int i = 0; i < table.Rows.Count; i++)
            {
                for (int j = 0; j < table.Columns.Count; j++)
                {
                    var value = table.Rows[i][j];

                    if (value is DateTime)
                    {
                        value = ((DateTime)value).ToString(datetime_format);
                    }

                    writer.Cells[i + 2, j + 1].Value = value;
                }
            }
        }

        public static string ToPerc(this decimal? value, int decimali = 0)
        {
            if (value == null)
            {
                return "-";
            }

            return value.Value.ToPerc(decimali);
        }

        public static string ToPerc(this decimal value, int decimali = 0)
        {                       
            return value.ToString("N" + decimali) + " %";
        }

        public static decimal Round(this decimal value, int decimali = 0)
        {
            return Math.Round(value, decimali);
        }

        public static void RemoveIfExists(this DataColumnCollection columns, string name)
        {
            if (columns.Contains(name))
            {
                columns.Remove(name);
            }
        }

        public static string ToJsParam(this string text)
        {
            if (text == null)
            {
                return "''";
            }

            return "'{0}'".FormatWith(text.Replace("'", "\\'").Replace("\"", "\\\"").ToHtml());
        }

        public static string ToJsParam(this Guid? guid)
        {
            return guid?.ToString().ToJsParam();
        }

        public static string ToJsParam(this Guid guid)
        {
            return guid.ToString().ToJsParam();
        }

        public static string ToJsParam(this int value)
        {
            return value.ToString();
        }

        public static List<int> GetUltimiXAnni(int num)
        {           
            return GetUltimiAnniFinoAl(DateTime.Now.Year - num);
        }

        public static List<int> GetUltimiAnniFinoAl(int fino_a_anno)
        {
            var anni = new List<int>();
            for (int anno = DateTime.Now.Year; anno >= fino_a_anno; anno--)
            {
                anni.Add(anno);
            }
            return anni;
        }

        public static List<KeyValuePair<int, string>> GetMesi()
        {
            var mesi = new List<KeyValuePair<int, string>>();
            for (int mese = 1; mese <= 12; mese++)
            {
                mesi.Add(new KeyValuePair<int, string>(mese, DateTimeFormatInfo.CurrentInfo.GetMonthName(mese).ToUpperFirstLetter()));
            }
            return mesi;
        }

        public static List<int> GetGiorniMese()
        {
            var giorni = new List<int>();
            for (int giorno = 1; giorno <= 31; giorno++)
            {
                giorni.Add(giorno);
            }
            return giorni;
        }

        public static List<int> GetGiorniMese(int anno, int mese)
        {
            var giorni = new List<int>();
            for (int giorno = 1; giorno <= DateTime.DaysInMonth(anno, mese); giorno++)
            {            
                giorni.Add(giorno);
            }
            return giorni;
        }

        //--- La LeftJoin è stata fatta per NetFramework potrebbe differire nel NET CORE
        public static IQueryable<TResult> LeftJoin<T1, T2, TKey, TResult>(this IQueryable<T1> table_1, IQueryable<T2> table_2, Expression<Func<T1, TKey>> table_1_key, Expression<Func<T2, TKey>> table_2_key, Expression<Func<LeftJoinTables<T1, T2>, TResult>> select)
        {
            return table_1.GroupJoin(table_2,
                table_1_key,
                table_2_key,
                (t1, t2) => new { t1, t2 })
            .SelectMany(x => x.t2.DefaultIfEmpty(), (x, t2) => new LeftJoinTables<T1, T2> { Table1 = x.t1, Table2 = t2 })
            .Select(select);
        }

        public class LeftJoinTables<T1, T2>
        {
            public T1 Table1 { get; set; }
            public T2 Table2 { get; set; }
        }

        public static decimal? NullIfZero(this decimal num) 
        {            
            return num == 0 ? (decimal?)null : num;
        }

        public static decimal? NullIfZero(this decimal? num)
        {
            return num == 0 ? (decimal?)null : num;
        }

        public static decimal? NullIfZero(this int num)
        {
            return num == 0 ? (int?)null : num;
        }

        public static int? NullIfZero(this int? num)
        {
            return num == 0 ? (int?)null : num;
        }

        public static List<int> IdStringToList(this string str)
        {
            if (str == null)
                return null;

            return str.Split('|').Select(x => (int)x.ToInt()).ToList();
        }

        public static string ToUpperFirstLetter(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return str;

            return char.ToUpper(str[0]) + str.Substring(1);
        }

        public static bool In<T>(this T? id, IEnumerable<T> items) where T : struct
        {
            return id.HasValue && id.Value.In(items);
        }

        public static bool In<T>(this T id, IEnumerable<T> items) where T : struct
        {
            return items.Contains(id);
        }

        public static bool UgualiAlMinuto(this DateTime a, DateTime b)
        {
            return a.Year == b.Year && a.Month == b.Month && a.Day == b.Day && a.Hour == b.Hour && a.Minute == b.Minute;
        }

        public static string EncryptString(string plainText, string key)
        {
            using(var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = new byte[16];

                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                    sw.Close();

                    return Convert.ToBase64String(ms.ToArray());
                }
            }

           
        }

        public static string DecryptString(string encryptedText, string key)
        {
            using(var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = new byte[16];

                using (var ms = new MemoryStream(Convert.FromBase64String(encryptedText)))
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        //private class LeftJoinIntermediate<T1, T2>
        //{
        //    public T1 Table1 { get; set; }
        //    public IEnumerable<T2> Table2 { get; set; }
        //}

    }
}
 
 