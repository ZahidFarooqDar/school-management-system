using AutoMapper;
using System.Drawing.Imaging;
using System.Drawing;
using Microsoft.EntityFrameworkCore;
using ZXing;
using ZXing.Common;
using SMSDAL.Context;
using SMSServiceModels.v1.General.ScanCodes;
using SMSServiceModels.Foundation.Base.Enums;
using SMSServiceModels.Foundation.Base.Interfaces;
using SMSServiceModels.Foundation.Base.CommonResponseRoot;
using SMSDomainModels.v1.General.ScanCodes;
using SMSBAL.Foundation.Base;
using SMSBAL.ExceptionHandler;

namespace SMSBAL.Projects.ScanCode
{
    public class ScanCodesProcess : CoreVisionBalOdataBase<ScanCodesFormatSM>
    {
        private readonly ILoginUserDetail _loginUserDetail;
        public ScanCodesProcess(IMapper mapper, ApiDbContext apiDbContext, ILoginUserDetail loginUserDetail)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
        }

        #region Odata
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override async Task<IQueryable<ScanCodesFormatSM>> GetServiceModelEntitiesForOdata()
        {
            var entitySet = _apiDbContext.ScanCodes;
            IQueryable<ScanCodesFormatSM> retSM = await MapEntityAsToQuerable<ScanCodesFormatDM, ScanCodesFormatSM>(_mapper, entitySet);
            return retSM;
        }
        #endregion Odata

        #region Count

        /// <summary>
        /// Get Barcode Format Data Count in database.
        /// </summary>
        /// <returns>integer response</returns>

        public async Task<int> GetAllBarcodeFormatCountResponse()
        {
            int resp = _apiDbContext.ScanCodes.Count();
            return resp;
        }

        #endregion Count

        #region Get All

        /// <summary>
        /// Asynchronously retrieves all Barcode Format Data entries from the database.
        /// </summary>
        /// <returns>
        /// A list of <see cref="ScanCodesFormatSM"/> objects representing all QR code data entries.
        /// </returns>
        public async Task<List<ScanCodesFormatSM>> GetAllScanCodes()
        {
            var dm = await _apiDbContext.ScanCodes.AsNoTracking().ToListAsync();

            List<ScanCodesFormatSM> res = new List<ScanCodesFormatSM>();

            foreach (var item in dm)
            {
                ScanCodesFormatSM ScanCodesFormatSM = _mapper.Map<ScanCodesFormatSM>(item);
                res.Add(ScanCodesFormatSM);
            }
            return res;
        }
        #endregion Get All

        #region Get By Id
        /// <summary>
        /// Retrieves QR code data for a specific ID asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the QR code data entry.</param>
        /// <returns>A <see cref="ScanCodesFormatSM"/> object representing the QR code data for the specified ID, or <c>null</c> if not found.</returns>
        public async Task<ScanCodesFormatSM> GetScanCodeById(int id)
        {
            var dm = await _apiDbContext.ScanCodes.FindAsync(id);
            if (dm == null)
            {
                return null;
            }
            var res = _mapper.Map<ScanCodesFormatSM>(dm);
            return res;
        }

        #endregion Get By Id

        #region Add/Update

        #region Add
        public async Task<ScanCodesFormatSM> AddBarcodeFormat(ScanCodesFormatSM objSM)
        {
            if (objSM == null)
            {
                return null;
            }
            var dm = _mapper.Map<ScanCodesFormatDM>(objSM);
            dm.CreatedBy = _loginUserDetail.LoginId;
            dm.CreatedOnUTC = DateTime.UtcNow;
            await _apiDbContext.ScanCodes.AddAsync(dm);
            if (await _apiDbContext.SaveChangesAsync() > 0)
            {
                return objSM;
            }
            throw new SMSException(ApiErrorTypeSM.Fatal_Log, "Something went wrong while Adding Barcode Format Data");
        }
        #endregion Add

        #region Update

        public async Task<ScanCodesFormatSM> UpdateBarcodeDetails(int id, ScanCodesFormatSM objSM)
        {
            if (id == null)
            {
                throw new SMSException(ApiErrorTypeSM.NoRecord_NoLog, $"Please Provide Value to Id", $"Please Provide Value to Id");
            }

            if (objSM == null)
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Nothing to Update", "Nothing to Update");
            }
            var objDM = await _apiDbContext.ScanCodes.FindAsync(id);

            string imageFullPath = null;
            string imageRelativePath = null;
            if (objDM != null)
            {
                objSM.Id = objDM.Id;
                var smProperties = objSM.GetType().GetProperties();
                var dmProperties = objDM.GetType().GetProperties();

                foreach (var smProperty in smProperties)
                {
                    var smValue = smProperty.GetValue(objSM, null);

                    var dmProperty = dmProperties.FirstOrDefault(p => p.Name == smProperty.Name);

                    if (dmProperty != null)
                    {
                        var dmValue = dmProperty.GetValue(objDM, null);

                        // Check if the value in objSM is null or empty, and update it with the corresponding value from objDM
                        if ((smValue == null || smValue is string strValue && string.IsNullOrEmpty(strValue)) && dmValue != null)
                        {
                            smProperty.SetValue(objSM, dmValue, null);
                        }
                    }
                }

                _mapper.Map(objSM, objDM);
                objDM.LastModifiedBy = _loginUserDetail.LoginId;
                objDM.LastModifiedOnUTC = DateTime.UtcNow;

                if (await _apiDbContext.SaveChangesAsync() > 0)
                {

                    var response = await GetScanCodeById(id);
                    return response;
                }
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Something went wrong while Updating Barcode Format Details", "Something went wrong while Updating Barcode Format Details");
            }
            else
            {
                throw new SMSException(ApiErrorTypeSM.Fatal_Log, $"Barcode Format not found: ", "Data to update not found, add as new instead.");
            }
        }

        #endregion Update

        #region Delete

        /// <summary>
        /// Deletes a barcode format by its Id from the database.
        /// </summary>
        /// <param name="id">The Id of the barcode format to be deleted.</param>
        /// <returns>
        /// A DeleteResponseRoot indicating whether the deletion was successful or not.
        /// </returns>
        public async Task<DeleteResponseRoot> DeleteBarcodeFormatById(int id)
        {
            // Check if a blog with the specified Id is present in the database
            var dm = await _apiDbContext.ScanCodes.FindAsync(id);


            if (dm != null)
            {

                _apiDbContext.ScanCodes.Remove(dm);

                // If save changes is successful, return a success response
                if (await _apiDbContext.SaveChangesAsync() > 0)
                {
                    return new DeleteResponseRoot(true, $"Barcode Format with Id {id} deleted successfully!");
                }
            }

            // If no blog with the specified Id is found, return a failure response
            return new DeleteResponseRoot(false, "Barcode Format not found");
        }

        #endregion Delete

        #endregion Add/Update

        #region Generate Barcode Data 
        /// <summary>
        /// Generates a barcode or QR code based on the provided input and format, returning it as a base64-encoded image string.
        /// </summary>
        /// <param name="objSM">An object containing the ID and data to be encoded into the barcode or QR code.</param>
        /// <returns>A <see cref="CodeResponseSM"/> object containing the base64-encoded image of the generated code.</returns>
        /// <exception cref="SMSException">
        /// Thrown when:
        /// - No data is found for the provided ID.
        /// - The barcode format is invalid.
        /// - An error occurs during barcode generation.
        /// </exception>
        public async Task<CodeResponseSM> GenerateCode(GenerateBarcodeSM objSM)
        {
            if (objSM == null)
            {
                return null;
            }
            var existingData = await GetScanCodeById(objSM.Id);
            if (existingData == null)
            {
                throw new SMSException(ApiErrorTypeSM.NoRecord_Log, $"No QR code data found for the provided ID: {objSM.Id}. Please verify the ID and try again.");
            }
            if (!Enum.TryParse(existingData.BarcodeFormat, out BarcodeFormat barcodeFormat))
            {
                throw new SMSException(ApiErrorTypeSM.InvalidInputData_Log, $"Invalid barcode format: {existingData.BarcodeFormat}. Please verify the format.");
            }
            // Create the barcode writer with appropriate settings
            var barcodeWriter = new BarcodeWriterPixelData
            {
                //Format = (BarcodeFormat)existingData.BarcodeFormat,
                Format = barcodeFormat,
                Options = new EncodingOptions
                {
                    Width = 256,
                    Height = 256,
                    Margin = 2,
                    PureBarcode = true
                }
            };

            try
            {
                var pixelData = barcodeWriter.Write(objSM.CodeData);
                // Create a bitmap using the generated pixel data
                using (var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb))
                {
                    using (var ms = new MemoryStream())
                    {
                        // Lock the bitmap's bits to write the pixel data
                        var bitmapData = bitmap.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                                                         ImageLockMode.WriteOnly,
                                                         PixelFormat.Format32bppRgb);
                        try
                        {
                            System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                        }
                        finally
                        {
                            bitmap.UnlockBits(bitmapData);
                        }

                        // Save the bitmap to the memory stream as a PNG image
                        bitmap.Save(ms, ImageFormat.Png);

                        // Convert the image to a base64 string
                        var base64Image = Convert.ToBase64String(ms.ToArray());
                        var res = new CodeResponseSM
                        {
                            Base64Image = base64Image,
                        };
                        return res;
                    }
                }
            }
            catch (Exception e)
            {

                throw new SMSException(ApiErrorTypeSM.Fatal_Log, existingData.ErrorData);
            }
        }

        #endregion Generate Barcode Data 

        #region QRCode
        /// <summary>
        /// Generates a QR code from the given input data and returns it as a base64-encoded image string.
        /// </summary>
        /// <param name="objSM">An object containing the data to be encoded into the QR code.</param>
        /// <returns>A <see cref="CodeResponseSM"/> object containing the base64-encoded image of the generated QR code.</returns>
        public async Task<CodeResponseSM> GenerateQRcode(GenerateQRCodeSM objSM)
        {
            // Create the barcode writer with appropriate settings
            var barcodeWriter = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Width = 256,
                    Height = 256,
                    Margin = 2,
                    PureBarcode = true
                }
            };

            // Generate the pixel data for the barcode
            var pixelData = barcodeWriter.Write(objSM.CodeData);

            // Create a bitmap using the generated pixel data
            using (var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb))
            {
                using (var ms = new MemoryStream())
                {
                    // Lock the bitmap's bits to write the pixel data
                    var bitmapData = bitmap.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                                                     ImageLockMode.WriteOnly,
                                                     PixelFormat.Format32bppRgb);
                    try
                    {
                        System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }

                    // Save the bitmap to the memory stream as a PNG image
                    bitmap.Save(ms, ImageFormat.Png);

                    // Convert the image to a base64 string
                    var base64Image = Convert.ToBase64String(ms.ToArray());
                    var res = new CodeResponseSM
                    {
                        Base64Image = base64Image,
                    };
                    return res;
                }
            }
        }

        #endregion QRCode
    }
}
