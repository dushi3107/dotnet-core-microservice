using itembank_index_backend.Models.Resources.Elastic;

namespace itembank_index_backend.Models.Responses;


// the response structure to client
public class ItemResourcesElasticResp
{
    public List<ItemResourceElastic> Items { get; set; }
    public long TotalElements { get; set; }
    public long TotalPages { get; set; }
    public long Size { get; set; }
    public long Number { get; set; }
}