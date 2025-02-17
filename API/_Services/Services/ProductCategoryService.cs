using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using API._Repositories.Interfaces;
using API._Services.Interfaces;
using API.Dtos;
using API.Helpers.Params;
using API.Helpers.Utilities;
using API.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace API._Services.Services
{
    public class ProductCategoryService : IProductCategoryService
    {
        private readonly IProductCategoryRepository _productCategoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IDropzoneService _dropzoneService;
        private readonly IMapper _mapper;
        private readonly MapperConfiguration _configuration;
        private OperationResult operationResult;

        public ProductCategoryService(
            IProductCategoryRepository productCategoryRepository,
            IMapper mapper,
            MapperConfiguration configuration,
            IProductRepository productRepository, 
            IDropzoneService dropzoneService)
        {
            _productCategoryRepository = productCategoryRepository;
            _mapper = mapper;
            _configuration = configuration;
            _productRepository = productRepository;
            _dropzoneService = dropzoneService;
        }

        public async Task<OperationResult> Create(ProductCategory_Dto model)
        {
            model.Product_Cate_ID = await GetProductCategoryID();
            var data = _mapper.Map<ProductCategory>(model);
            try
            {
                _productCategoryRepository.Add(data);
                await _productCategoryRepository.Save();
                operationResult = new OperationResult { Success = true, Message = "Product Category was successfully added." };
            }
            catch (Exception)
            {
                operationResult = new OperationResult { Success = false, Message = "Product Category was exists." };
            }
            return operationResult;
        }

        public async Task<ProductCategory_Dto> GetProductCategoryByID(string productCateID)
        {
            return await _productCategoryRepository.FindAll(x => x.Product_Cate_ID == productCateID)
            .ProjectTo<ProductCategory_Dto>(_configuration).FirstOrDefaultAsync();
        }

        public async Task<List<ProductCategory_Dto>> GetAllAsync()
        {
            return await _productCategoryRepository.FindAll().ProjectTo<ProductCategory_Dto>(_configuration).ToListAsync();
        }

        public async Task<PageListUtility<ProductCategory_Dto>> GetProductCategoryWithPaginations(PaginationParams param, string text, bool isPaging = true)
        {
            var data = _productCategoryRepository.FindAll().ProjectTo<ProductCategory_Dto>(_configuration).OrderByDescending(x => x.Update_Time);
            if (!string.IsNullOrEmpty(text))
            {
                data = data.Where(x => x.Product_Cate_Name.ToLower().Contains(text.ToLower())
                || x.Update_By.ToLower().Contains(text.ToLower())
                || x.Product_Cate_ID.ToLower().Contains(text.ToLower())).OrderByDescending(x => x.Update_Time);
            }
            return await PageListUtility<ProductCategory_Dto>.PageListAsync(data, param.PageNumber, param.PageSize, isPaging);
        }

        public async Task<OperationResult> Update(ProductCategory_Dto model)
        {
            var data = _mapper.Map<ProductCategory>(model);
            try
            {
                _productCategoryRepository.Update(data);
                await _productCategoryRepository.Save();
                operationResult = new OperationResult { Success = true, Message = "Product Category was successfully updated." };
            }
            catch (Exception)
            {
                operationResult = new OperationResult { Success = false, Message = "Product Category was failes updated." };
            }

            return operationResult;
        }

        public async Task<OperationResult> Remove(List<ProductCategory_Dto> model)
        {
            List<ProductCategory> productCates = new List<ProductCategory>();
            List<Product> products = new List<Product>();
            foreach (var item in model)
            {
                productCates.Add(await _productCategoryRepository.FindAll(x => x.Product_Cate_ID == item.Product_Cate_ID).FirstOrDefaultAsync());
                products.AddRange(await _productRepository.FindAll(x => x.Product_Cate_ID == item.Product_Cate_ID).ToListAsync());
            }

            if (productCates != null && productCates.Count > 0)
            {
                try
                {
                    if (products != null && products.Count > 0)
                    {
                        foreach (var product in products)
                        {
                            var images = await _productRepository.FindAll(x => x.Product_Cate_ID == product.Product_Cate_ID &&
                                                    x.Product_ID == product.Product_ID &&
                                                    x.Product_Name == product.Product_Name).Select(x => x.FileImages).Distinct().FirstOrDefaultAsync();

                            var videos = await _productRepository.FindAll(x => x.Product_Cate_ID == product.Product_Cate_ID &&
                                                                    x.Product_ID == product.Product_ID &&
                                                                    x.Product_Name == product.Product_Name).Select(x => x.FileVideos).Distinct().FirstOrDefaultAsync();
                            if (!string.IsNullOrEmpty(images))
                            {
                                _dropzoneService.DeleteFileUpload(images, "\\uploaded\\images\\product");
                            }
                            if (!string.IsNullOrEmpty(videos))
                            {
                                _dropzoneService.DeleteFileUpload(videos, "\\uploaded\\video\\product");
                            }
                        }
                        _productRepository.RemoveMultiple(products);
                    }
                    _productCategoryRepository.RemoveMultiple(productCates);
                    await _productCategoryRepository.Save();
                    return operationResult = new OperationResult { Success = true, Message = "Product Category was delete successfully." };
                }
                catch (Exception)
                {
                    return operationResult = new OperationResult { Success = false, Message = "Product Category was delete failes." };
                }
            }
            else
            {
                return operationResult = new OperationResult { Success = false, Message = "Product Category was delete failes." };
            }
        }

        public async Task<string> GetProductCategoryID()
        {
            string productCateID = "Product";
            var items = await _productCategoryRepository.FindAll(x => x.Product_Cate_ID.Contains(productCateID))
                .OrderByDescending(x => x.Product_Cate_ID).FirstOrDefaultAsync();
            if (items != null)
            {
                var serinumber = items.Product_Cate_ID.Substring(8).ToInt();
                var tmp = (serinumber >= 999) ? (serinumber + 1).ToString() :
                    (serinumber >= 99) ? ("0" + (serinumber + 1)) :
                        (serinumber < 9) ? ("000" + (serinumber + 1)) : ("00" + (serinumber + 1));
                productCateID = "Product" + tmp;
            }
            else
            {
                productCateID = "Product0001";
            }
            return productCateID;
        }

        public async Task<object> GetIDAndName()
        {
            var data = await _productCategoryRepository.FindAll()
            .Select(x => new { Id = x.Product_Cate_ID, Name = x.Product_Cate_ID + "_" + x.Product_Cate_Name }).ToListAsync();
            return data;
        }

        public async Task<OperationResult> ImportExcel(string pathFile, string user)
        {
            using (var package = new ExcelPackage(new FileInfo(pathFile)))
            {
                ExcelWorksheet workSheet = package.Workbook.Worksheets[0];
                int totalRows = workSheet.Dimension.Rows;
                for (int i = 2; i <= totalRows; i++)
                {
                    ProductCategory_Dto productCategory = new ProductCategory_Dto();
                    productCategory.Product_Cate_Name = workSheet.Cells[i, 1].Value.ToSafetyString().Trim();
                    productCategory.Status = workSheet.Cells[i, 2].Value.ToBool();
                    productCategory.Position = workSheet.Cells[i, 3].Value.ToInt();
                    productCategory.Update_By = user;
                    productCategory.Update_Time = DateTime.Now;

                    try
                    {
                        await Create(productCategory);
                        operationResult = new OperationResult { Message = "Import Success", Success = true };
                    }
                    catch
                    {
                        operationResult = new OperationResult { Message = "Import Faild", Success = false };
                    }
                }
            }
            return await Task.FromResult(operationResult);
        }
    }
}