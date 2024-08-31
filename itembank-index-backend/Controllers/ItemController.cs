using itembank_index_backend.Models.Requests;
using itembank_index_backend.Models.Resources.Elastic;
using itembank_index_backend.Services;
using itembank_index_backend.Utils;
using Microsoft.AspNetCore.Mvc;

namespace itembank_index_backend.Controllers;

[ApiController]
[Route("[controller]")]
public class ItemController : ControllerBase
{
    private readonly ItemService _itemService;

    public ItemController(ItemService itemService)
    {
        _itemService = itemService;
    }

    /**
     * 透過請求中給定的條件取得搜尋結果，用於總庫搜尋結果頁面("再"搜尋時使用，換頁or排序)
     */
    [HttpPost("search", Name = "Search")]
    public async Task<dynamic> Search([FromBody] ItemConditionReq itemConditionReq)
    {
        if (_itemService.InvalidConditionContent(itemConditionReq))
        {
            return Http.ItembankSearchBadRequest;
        }

        var result = await _itemService.ConditionSearchAsync(itemConditionReq);

        return Http.HandleNullResponse(result);
    }

    /**
     * 儲存請求中的搜尋條件，並回傳一組搜尋ID，下次可以使用此搜尋ID取回搜尋條件，用於總庫搜尋條件頁面(表單提交)
     */
    [HttpPost("index", Name = "Index")]
    public async Task<dynamic> Index([FromBody] ItemConditionReq itemConditionReq)
    {
        if (_itemService.InvalidConditionContent(itemConditionReq, false))
        {
            return Http.ItembankSearchBadRequest;
        }

        var result = await _itemService.SetConditionIndexAsync(itemConditionReq);

        return Http.HandleNullResponse(result);
    }

    /**
     * 藉由請求的搜尋ID取得已儲存的搜尋條件，用於總庫搜尋的兩個頁面(載入頁面時先取回已儲存的搜尋條件)
     */
    [HttpGet("index", Name = "Index")]
    public async Task<dynamic> Index([FromQuery] string recordId)
    {
        var result = await _itemService.GetConditionIndexAsync(recordId);

        return Http.HandleNullResponse(result);
    }

    /**
     * 藉由請求的搜尋ID取得已儲存的搜尋條件，並透過該搜尋條件進行搜尋(結合 /index, /search)，用於總庫搜尋結果頁面顯示結果
     */
    [HttpGet("index/search", Name = "IndexSearch")]
    public async Task<dynamic> IndexSearch([FromQuery] string recordId)
    {
        ItemConditionReq itemConditionReq = await _itemService.GetConditionIndexAsync(recordId);
        if (_itemService.InvalidConditionContent(itemConditionReq))
        {
            return Http.ItembankSearchBadRequest;
        }

        var result = await _itemService.ConditionSearchAsync(itemConditionReq);

        return Http.HandleNullResponse(result);
    }

    /**
     * 透過給定的條件進行搜尋只回傳題目ID，用於總庫搜尋結果頁面(下載題目ID)
     */
    [HttpPost("ids", Name = "Ids")]
    public async Task<dynamic> Ids([FromBody] ItemConditionReq itemConditionReq)
    {
        if (_itemService.InvalidConditionContent(itemConditionReq))
        {
            return Http.ItembankSearchBadRequest;
        }

        var result = await _itemService.ConditionSearchIdsAsync(itemConditionReq);

        return Http.HandleNullResponse(result);
    }

    /**
     * 寫入資料
     */
    [HttpPost("write", Name = "Write")]
    public async Task<dynamic> Write([FromBody] ItemResourceElastic item)
    {
        var result = await _itemService.WriteOrUpdateAsync(item);

        return Http.HandleNullResponse(result);
    }

    /**
     * 大量資料寫入
     */
    [HttpPost("write/multi", Name = "WriteMulti")]
    public async Task<dynamic> WriteMulti([FromBody] List<ItemResourceElastic> items)
    {
        var result = await _itemService.WriteOrUpdateMultiAsync(items);

        return Http.HandleNullResponse(result);
    }
}