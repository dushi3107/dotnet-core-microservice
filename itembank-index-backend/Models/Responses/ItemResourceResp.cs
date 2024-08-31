namespace itembank_index_backend.Models.Responses;

public class ItemResourceResp
{
    public List<Content>? content { get; set; }
    public int? number { get; set; }
    public int? size { get; set; }
    public int? numberOfElements { get; set; }
    public int? totalElements { get; set; }
    public int? totalPages { get; set; }
    public bool? hasContent { get; set; }
    public bool? hasNextPage { get; set; }
    public bool? hasPreviousPage { get; set; }
    public bool? isFirstPage { get; set; }
    public bool? isLastPage { get; set; }
}

// this content involves items 
public class Content
{
    public Content()
    {
        applicableYears = new List<int>();
        subjectIds = new List<string>();
        content = new ItemContent();
        metadata = new Dictionary<string, string>();
        resourceLinks = new List<ResourceLink>();
        bodyOfKnowledges = new List<BodyOfKnowledge>();
    }

    public string id { get; set; }
    public string? fidelity { get; set; }
    public string? difficulty { get; set; }
    public List<int>? applicableYears { get; set; }
    public List<string>? subjectIds { get; set; }
    // public string? preamble { get; set; }
    public ItemContent? content { get; set; }
    public string? solution { get; set; }
    // public string? importRemark { get; set; }
    public bool isSet { get; set; }
    // public string? correctness { get; set; }
    // public bool? isCorrect { get; set; }
    // public bool? isIncorrect { get; set; }
    public bool isOnlineReady { get; set; }

    public string? onlineReadiness { get; set; }

    // public List<string>? labels { get; set; }
    public Dictionary<string, string>? metadata { get; set; }

    // public List<Property>? properties { get; set; }
    // public List<QuestionInfo>? questions { get; set; }
    // public int? questionCount { get; set; }
    // public int? answerCount { get; set; }
    // public List<ProductStatus>? productStatuses { get; set; }
    public List<ResourceLink>? resourceLinks { get; set; }
    // public List<ResourceManifest>? resourceManifest { get; set; }
    // public string? issues { get; set; }
    // public List<string>? duplicates { get; set; }
    public string? createdOn { get; set; }
    public string? updatedOn { get; set; }
    // public string? log { get; set; }
    public List<BodyOfKnowledge>? bodyOfKnowledges { get; set; }
    // public string? identifier { get; set; }
    // public string? href { get; set; }
    // public List<Link>? links { get; set; }
}

public class ItemContent
{
    public string? id { get; set; }
    public string? preamble { get; set; }
    public List<Question>? questions { get; set; }
    public int? questionCount { get; set; }
    public int? answerCount { get; set; }
}

public class Question
{
    public string? stem { get; set; }
    public string? solution { get; set; }
    public List<string>? options { get; set; }
    public List<List<string>>? answers { get; set; }
    public List<List<string>>? proposeAnswers { get; set; }
    public Dictionary<string, string>? supplementals { get; set; }
    public string? optionFirstLetter { get; set; }
    public List<bool>? latexAnswers { get; set; }
    public string? answeringMethod { get; set; }
}

public class Property
{
    public string? name { get; set; }
    public List<string>? values { get; set; }
}

public class QuestionInfo
{
    public int? questionIndex { get; set; }
    public string? answeringMethod { get; set; }
    public string? difficulty { get; set; }
    public bool? isSequenceOption { get; set; }
    public List<string>? subjectIds { get; set; }
    public string? answers { get; set; }
    public string? createdOn { get; set; }
    public string? updatedOn { get; set; }
}

public class ProductStatus
{
    public string? status { get; set; }
    public string? comment { get; set; }
    public string? target { get; set; }
    public string? updatedBy { get; set; }
}

public class ResourceLink
{
    public string? name { get; set; }
    public string? rel { get; set; }
    public string? href { get; set; }
    public string? contentType { get; set; }
}

public class ResourceManifest
{
    public string? id { get; set; }
    public string? name { get; set; }
    public string? contentType { get; set; }
    public int? size { get; set; }
}

public class BodyOfKnowledge
{
    public string? subjectId { get; set; }
    public int? initiationYear { get; set; }
    public int? finalYear { get; set; }
    public string? code { get; set; }
    public string? name { get; set; }
    // public string? description { get; set; }
    // public List<string>? composition { get; set; }
    // public string? href { get; set; }
    // public string? id { get; set; }
    // public List<Link>? links { get; set; }
}

public class Link
{
    public string? href { get; set; }
    public string? rel { get; set; }
    public string? name { get; set; }
    public string? method { get; set; }
}