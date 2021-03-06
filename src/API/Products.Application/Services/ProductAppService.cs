﻿using AutoMapper;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Products.Application.Interfaces;
using Products.Domain.DTO;
using Products.Domain.Interfaces;
using Products.Domain.Models;
using Products.Domain.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Products.Application.Services
{
    public class ProductAppService : IProductAppService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IProductRepository _productRepository;

        public ProductAppService(IUnitOfWork uow, IMapper mapper, IProductRepository productRepository)
        {
            _uow = uow;
            _mapper = mapper;
            _productRepository = productRepository;
        }

        public async Task Add(ProductDto productDto)
        {
            var product = _mapper.Map<Product>(productDto);
            product.CreationDate = DateTime.Now;
            product.LastModified = product.CreationDate;
            product.Active = true;

            _productRepository.Add(product);
            await _uow.Commit();
        }

        public async Task<PaginationResultDTO<ProductDto>> Filter(ProductParameters parameters)
        {
            var query = _productRepository.GetAllAsQueryable();            

            if (!string.IsNullOrEmpty(parameters.Name))
            {
                query = query.Where(x => x.Name.ToLower().Contains(parameters.Name.ToLower()));
            }

            var list = await query.OrderBy(x => x.Name)
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .ToListAsync();

            var result = new PaginationResultDTO<ProductDto>
            {
                Result = _mapper.Map<IEnumerable<ProductDto>>(list),
                CollectionSize = await query.CountAsync()
            };

            return result;
        }

        public async Task<ProductDto> GetById(Guid id)
        {
            var result = _mapper.Map<ProductDto>(await _productRepository.GetById(id));
            return result;
        }

        public async Task<bool> Update(Guid id, ProductDto productDto)
        {
            var count = await _productRepository.Count(Builders<Product>.Filter.Eq(x => x.Id, id));

            if (count == 0) return false;

            var filter = Builders<Product>.Filter.Eq(x => x.Id, id);

            var update = Builders<Product>.Update
                .Set(x => x.Name, productDto.Name)
                .Set(x => x.Description, productDto.Description)
                .Set(x => x.Price, productDto.Price)
                .Set(x => x.Active, productDto.Active)
                .CurrentDate(x => x.LastModified);

            _productRepository.Update(filter, update);
            
            await _uow.Commit();

            return true;
        }

        public async Task Delete(Guid id)
        {
            _productRepository.Delete(Builders<Product>.Filter.Eq(x => x.Id, id));
            await _uow.Commit();
        }
    }
}
