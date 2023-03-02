using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contracts;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Catalog.Service.Extensions;
using Play.Common;

namespace Play.Catalog.Service.Controllers
{


    [Route("items")]
    [ApiController]
    //[Authorize(Roles = AdminRole)]
    public class ItemsController : ControllerBase
    {
        private const string AdminRole = "Admin";

        private readonly IRepository<Item> itemsRepository;
        private readonly IPublishEndpoint publishEndpoint;

        public ItemsController(IRepository<Item> itemsRepository, IPublishEndpoint publishEndpoint)
        {
            this.itemsRepository = itemsRepository;
            this.publishEndpoint = publishEndpoint;
        }

        //GET /items
        [HttpGet]
        [Authorize(Policies.Read)]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
        {
            var items = (await itemsRepository.GetAllAsync())
                        .Select(item => item.AsDto());
            return Ok(items);
        }


        //GET /items/{id}
        [HttpGet("{id}")]
        [Authorize(Policies.Read)]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await itemsRepository.GetAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return item.AsDto();
        }


        //POST /items
        [HttpPost]
        [Authorize(Policies.Write)]
        public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto createItemDto)
        {
            var item = new Item
            {
                Name = createItemDto.Name,
                Price = createItemDto.Price,
                Description = createItemDto.Description,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await itemsRepository.CreateAsync(item);

            await publishEndpoint.Publish(
                new CatalogItemCreated(
                    item.Id, 
                    item.Name, 
                    item.Description, 
                    item.Price
                    ));

            return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item);
        }

        //PUT /items/{id}
        [HttpPut]
        [Authorize(Policies.Write)]
        public async Task<ActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
        {
            var existingItem = await itemsRepository.GetAsync(id);

            if (existingItem == null)
            {
                return NotFound();
            }

            existingItem.Name = updateItemDto.Name;
            existingItem.Description = updateItemDto.Description;
            existingItem.Price = updateItemDto.Price;

            await itemsRepository.UpdateAsync(existingItem);

            await publishEndpoint.Publish(
                new CatalogItemUpdated(
                    existingItem.Id,
                    existingItem.Name,
                    existingItem.Description, 
                    existingItem.Price
                    ));

            return NoContent();
        }

        //DELETE /items/{id}
        [HttpDelete("{id}")]
        [Authorize(Policies.Write)]
        public async Task<ActionResult> DeleteAsync(Guid id)
        {
            var item = await itemsRepository.GetAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            await itemsRepository.RemoveAsync(item.Id);

            await publishEndpoint.Publish(new CatalogItemDeleted(item.Id));

            return NoContent();
        }
    }
}
