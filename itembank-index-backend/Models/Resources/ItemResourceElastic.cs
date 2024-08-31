using System.Text.Json.Serialization;
using itembank_index_backend.Models.Definition;

namespace itembank_index_backend.Models.Resources.Elastic;

public class ItemIdsElastic
{
    public List<string> Ids { get; set; }
    public long TotalElements { get; set; }
}

// the response structure from Elastic
public class ItemResourceElastic
{
    /**
     * replace all the reserved words of Elastic that its filed type is
     * "text",
     * use this before doing search
     */
    public void MappingReservedWord()
    {
        // replace, if those text fields required the reserved words search
        foreach (var mapping in ElasticReservedWord.Mappings)
        {
            var key = mapping.Key;
            var value = mapping.Value;

            if (!string.IsNullOrEmpty(EditorRemark))
            {
                EditorRemark = EditorRemark.Replace(key, value);
            }

            if (!string.IsNullOrEmpty(Preamble))
            {
                Preamble = Preamble.Replace(key, value);
            }

            if (ProductStatuses != null && ProductStatuses.Count > 0)
            {
                foreach (var status in ProductStatuses)
                {
                    if (!string.IsNullOrEmpty(status.Comment))
                    {
                        status.Comment = status.Comment.Replace(key, value);
                    }
                }
            }

            if (Questions != null && Questions.Count > 0)
            {
                foreach (var question in Questions)
                {
                    if (!string.IsNullOrEmpty(question.Stem))
                    {
                        question.Stem = question.Stem.Replace(key, value);
                    }

                    if (question.AnswerKeywords != null && question.AnswerKeywords.Count > 0)
                    {
                        for (int i = 0; i < question.AnswerKeywords.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(question.AnswerKeywords[i]))
                            {
                                question.AnswerKeywords[i] = question.AnswerKeywords[i].Replace(key, value);
                            }
                        }
                    }

                    if (question.Options != null && question.Options.Count > 0)
                    {
                        for (int i = 0; i < question.Options.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(question.Options[i]))
                            {
                                question.Options[i] = question.Options[i].Replace(key, value);
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(Solution))
            {
                Solution = Solution.Replace(key, value);
            }
        }
    }

    public string Id { get; set; }
    public List<string>? ProductCodes { get; set; }
    public List<string>? LabelNames { get; set; }
    public List<string>? Sources { get; set; }
    public List<string>? PublishSources { get; set; }
    public List<string>? VersionIds { get; set; }
    public List<string>? VolumeNames { get; set; }
    public List<string>? DocumentIds { get; set; }
    public List<string>? DocumentRepositoryIds { get; set; }
    public List<string>? RegularKnowledgeIds { get; set; }
    public List<string>? RegularKnowledgeCodes { get; set; }
    public List<string>? RegularLessonIds { get; set; }
    public List<string>? RegularLessonCodes { get; set; }
    public List<string>? DiscreteKnowledgeIds { get; set; }
    public List<string>? DiscreteKnowledgeCodes { get; set; }
    public List<string>? DiscreteLessonIds { get; set; }
    public List<string>? DiscreteLessonCodes { get; set; }
    public List<string>? PreambleKnowledgeCodes { get; set; }
    public List<string>? PreambleKnowledgeIds { get; set; }
    public List<string>? QuestionKnowledgeCodes { get; set; }
    public List<string>? QuestionKnowledgeIds { get; set; }
    public List<string>? OptionKnowledgeCodes { get; set; }
    public List<string>? OptionKnowledgeIds { get; set; }
    public List<string>? AbilityCodes { get; set; }
    public List<string>? AbilityIds { get; set; }
    public List<string>? PreambleAbilityCodes { get; set; }
    public List<string>? PreambleAbilityIds { get; set; }
    public List<string>? QuestionAbilityCodes { get; set; }
    public List<string>? QuestionAbilityIds { get; set; }
    public List<string>? OptionAbilityCodes { get; set; }
    public List<string>? OptionAbilityIds { get; set; }
    public List<string>? RecognitionCodes { get; set; }
    public List<string>? RecognitionIds { get; set; }
    public List<string>? PreambleRecognitionCodes { get; set; }
    public List<string>? PreambleRecognitionIds { get; set; }
    public List<string>? QuestionRecognitionCodes { get; set; }
    public List<string>? QuestionRecognitionIds { get; set; }
    public List<string>? OptionRecognitionCodes { get; set; }
    public List<string>? OptionRecognitionIds { get; set; }
    public List<string>? SubjectIds { get; set; }
    public List<string>? BodyOfKnowledgeCodes { get; set; }
    public List<string>? BodyOfKnowledgeIds { get; set; }
    public List<ItemYear>? ItemYears { get; set; }
    public List<Product>? Products { get; set; }
    public List<Catalog>? Catalogs { get; set; }
    public List<ContentSection>? ContentSections { get; set; }
    public List<ProductStatus>? ProductStatuses { get; set; }

    public List<string>? PreambleLessonCodes { get; set; }
    public List<string>? PreambleLessonIds { get; set; }
    public List<string>? QuestionLessonCodes { get; set; }
    public List<string>? QuestionLessonIds { get; set; }
    public List<string>? OptionLessonCodes { get; set; }
    public List<string>? OptionLessonIds { get; set; }
    public List<string>? ContentSectionCodes { get; set; }
    public List<string>? ContentSectionIds { get; set; }
    public string? UserType { get; set; }
    public List<string>? UserTypes { get; set; }
    public List<string>? FileNames { get; set; }
    public List<string>? ImportRecordIds { get; set; }
    public string? Correctness { get; set; }
    public string? OnlineReadiness { get; set; }
    public List<string>? CatalogTags { get; set; }
    public List<string>? CatalogIds { get; set; }
    public bool? IsLiteracy { get; set; }
    public string? Copyright { get; set; }
    public bool? IsSet { get; set; }
    public bool? IsCorrect { get; set; }
    public string? Solution { get; set; }
    public string? Preamble { get; set; }
    public string? EditorRemark { get; set; }
    public string? Topic { get; set; }

    public bool? HasVideoUrls { get; set; }
    public string? Identifier { get; set; }
    public List<Question>? Questions { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }

    public List<Property>? Properties { get; set; }
}

public class Question
{
    public string? Stem { get; set; }
    public List<string>? Options { get; set; }
    public List<string>? AnswerKeywords { get; set; }
    public string? AnsweringMethod { get; set; }
    public List<string>? Answers { get; set; }
    public List<string>? ProposeAnswers { get; set; }
    public List<bool>? LatexAnswers { get; set; }
}

public class ItemYear
{
    public List<string>? BodyOfKnowdedgeCodes { get; set; }
    public string? Year { get; set; }
    public List<string>? DimensionValueIds { get; set; }
    public List<string>? UsageTypes { get; set; }
}

public class Product
{
    public string? Code { get; set; }
    public string? Year { get; set; }
}

public class Catalog
{
    public string? Id { get; set; }
    public bool? IsShared { get; set; }
    public List<string>? Sources { get; set; }
    public List<string>? UserTypes { get; set; }
}

public class ContentSection
{
    public string? Volume { get; set; }
    public string? Year { get; set; }
    public string? Version { get; set; }
    public string? ContentId { get; set; }
    public string? Id { get; set; }
    public string? Code { get; set; }
    public string? SubjectId { get; set; }
}

public class ProductStatus
{
    public string? Status { get; set; }
    public string? Target { get; set; }
    public string? Comment { get; set; }
}

public class Property
{
    public string? Name { get; set; }
    public List<string>? Values { get; set; }
}

public class ItemRecordsElastic
{
    [JsonInclude]
    [JsonPropertyName("data")]
    public string Data { get; set; }
}