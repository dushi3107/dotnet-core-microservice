namespace itembank_index_backend.Models.Resources.Sql;

public class ItemResourceSql
{
    // table: itemYears
    public int? applicableYear { get; set; }

    // table: bodiesOfknowledge
    public int? bodyOfKnowledgeId { get; set; }
    public string? bodyOfKnowledgeCode { get; set; }
    public string? bodyOfKnowledgeName { get; set; }
    public string? bodyOfKnowledgeSubjectId { get; set; }
    public int? bodyOfKnowledgeInitiationYear { get; set; }

    public int? bodyOfKnowledgeFinalYear { get; set; }

    // table: items
    public string? content { get; set; } // should be deserialized to object

    // public string? correctness { get; set; }
    public string? createdOn { get; set; }
    public string? difficulty { get; set; }
    public string? fidelity { get; set; }
    public string id { get; set; }
    public string? metadata { get; set; } // should be transformed to dictionary, Dictionary<string, string>
    public string? onlineReadiness { get; set; }
    public string? resourceLinks { get; set; } // should be transformed to list, List<ResourceLink>
    public string? solution { get; set; }
    public string? subjectIds { get; set; } // EX: "id1, id2", should be divided
    public string? updatedOn { get; set; }
    
    //table: questions
    public string? AnsweringMethod { get; set; }
    public int? QuestionIndex { get; set; }

    // public bool? isSet { get; set; } // content.questions.count > 0 || content.preamble != null
    // public bool? isOnlineReady { get; set; } // onlineReadiness == "ready"
}