using Newtonsoft.Json;

namespace itembank_index_backend.Models.Resources.S3;

public class ItemResourceS3
{
    public string Id { get; set; }
    public IList<QuestionResource> Questions { get; set; }
    public ItemContentResource Content { get; set; }
    public int QuestionCount { get; set; }
    public List<ResourceLink> ResourceLinks { get; set; }
    public ResourceLink Audio { get { return ResourceLinks?.Where(x => x.Rel == "audio").FirstOrDefault(); } }
    public string UserType { get; set; }
    public int AnswerCount { get; set; }

    public static implicit operator ItemResourceS3(string json)
    {
        return JsonConvert.DeserializeObject<ItemResourceS3>(json);
    }
}

public class QuestionResource
{
    public string AnsweringMethod { get; set; }
    public int QuestionIndex { get; set; }
    public int Difficulty { get; set; }
}

public class ItemContentResource
{
    public IList<QuestionContentResource> Questions { get; set; }
}

public class QuestionContentResource
{
    public IList<IList<string>> Answers { get; set; }
}

public class ResourceLink
{
    public string Name { get; set; }
    public string Rel { get; set; }
}