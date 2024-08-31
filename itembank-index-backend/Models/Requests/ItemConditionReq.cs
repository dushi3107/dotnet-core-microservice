using itembank_index_backend.Models.Definition;

namespace itembank_index_backend.Models.Requests;

// from client request
public class ItemConditionReq
{
    /**
     * replace all the reserved words of Elastic that its filed type is
     * "text",
     * use this before doing search
     */
    public void MappingReservedWord(bool reverse = false)
    {
        var mappings = reverse ? ElasticReservedWord.MappingsReverse : ElasticReservedWord.Mappings;

        // replace, if those text fields required the reserved words search
        foreach (var mapping in mappings)
        {
            var key = mapping.Key;
            var value = mapping.Value;

            if (EditorRemarks != null && EditorRemarks.Count > 0)
            {
                for (int i = 0; i < EditorRemarks.Count; i++)
                {
                    EditorRemarks[i] = EditorRemarks[i].Replace(key, value);
                }
            }

            if (NeEditorRemarks != null && NeEditorRemarks.Count > 0)
            {
                for (int i = 0; i < NeEditorRemarks.Count; i++)
                {
                    NeEditorRemarks[i] = NeEditorRemarks[i].Replace(key, value);
                }
            }

            if (SearchTexts != null && SearchTexts.Count > 0)
            {
                for (int i = 0; i < SearchTexts.Count; i++)
                {
                    SearchTexts[i] = SearchTexts[i].Replace(key, value);
                }
            }

            if (MustSearchTexts != null && MustSearchTexts.Count > 0)
            {
                for (int i = 0; i < MustSearchTexts.Count; i++)
                {
                    MustSearchTexts[i] = MustSearchTexts[i].Replace(key, value);
                }
            }

            if (NeMustSearchTexts != null && NeMustSearchTexts.Count > 0)
            {
                for (int i = 0; i < NeMustSearchTexts.Count; i++)
                {
                    NeMustSearchTexts[i] = NeMustSearchTexts[i].Replace(key, value);
                }
            }
        }
    }

    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public bool? ignoreDuplicated { get; set; }

    public List<string>? NeLabelNames { get; set; }

    // 科目
    public string? SubjectId { get; set; }

    // 入學學程
    public string? BodyOfKnowledgeCode { get; set; }

    // 出處
    public List<string>? PublishSources { get; set; }

    // 出處-剔除
    public List<string>? NePublishSources { get; set; }

    // 課名
    public List<string>? LessonIds { get; set; }

    // 課名-剔除
    public List<string>? NeLessonIds { get; set; }

    // 關鍵字(聯集)
    public List<string>? SearchTexts { get; set; }

    // 關鍵字(聯集)-剔除
    public List<string>? NeSearchTexts { get; set; }

    // 關鍵字(交集)
    public List<string>? MustSearchTexts { get; set; }

    // 關鍵字(交集)-剔除
    public List<string>? NeMustSearchTexts { get; set; }

    // 目錄(區分年度、版本)
    public List<string>? CatalogIds { get; set; }

    // 目錄(區分年度、版本)-剔除
    public List<string>? NeCatalogIds { get; set; }

    // 知識向度
    public List<string>? KnowledgeIds { get; set; }

    // 知識向度-剔除
    public List<string>? NeKnowledgeIds { get; set; }

    // 適用學年度
    public List<string>? ItemYears { get; set; }

    // 適用學年度-剔除
    public List<string>? NeItemYears { get; set; }

    // 編輯備註
    public List<string>? EditorRemarks { get; set; }

    // 編輯備註-剔除
    public List<string>? NeEditorRemarks { get; set; }
    public string SortField { get; set; }
    public bool Ascending { get; set; }

    // Advanced Conditions
    // 產品(不分年度、版本)
    public List<string>? ProductCodes { get; set; }

    // 題目id
    public List<string>? Ids { get; set; }

    // 題目id-剔除
    public List<string>? NeIds { get; set; }

    // 來源
    public List<string>? Sources { get; set; }

    // 來源-剔除
    public List<string>? NeSources { get; set; }

    // 版本
    public List<string>? VersionIds { get; set; }

    // 版本-剔除
    public List<string>? NeVersionIds { get; set; }

    // 必出知識向度
    public List<string>? DiscreteKnowledgeIds { get; set; }

    // 必出知識向度-剔除
    public List<string>? NeDiscreteKnowledgeIds { get; set; }

    // 認知向度
    public List<string>? RecognitionIds { get; set; }

    // 認知向度-剔除
    public List<string>? NeRecognitionIds { get; set; }

    // 必出課名
    public List<string>? DiscreteLessonIds { get; set; }

    // 必出課名-剔除
    public List<string>? NeDiscreteLessonIds { get; set; }

    // 題型
    public List<string>? UserTypes { get; set; }

    // 題型-剔除
    public List<string>? NeUserTypes { get; set; }

    // 作答方式
    public List<string>? AnsweringMethods { get; set; }

    // 作答方式-剔除
    public List<string>? NeAnsweringMethods { get; set; }

    // 答案
    public List<string>? Answers { get; set; }

    // 入庫檔名
    public List<string>? FileNames { get; set; }

    // 入庫檔名-剔除
    public List<string>? NeFileNames { get; set; }

    // 議題
    public List<string>? Topics { get; set; }

    // 檔案id(import id)
    public List<string>? ImportRecordIds { get; set; }

    // 檔案id(import id)-剔除
    public List<string>? NeImportRecordIds { get; set; }

    // 文件id(document id)
    public List<string>? DocumentIds { get; set; }

    // 文件id(document id)-剔除
    public List<string>? NeDocumentIds { get; set; }

    // 五欄資料夾id(document repository id)
    public List<string>? DocumentRepositoryIds { get; set; }

    // 五欄資料夾id(document repository id)-剔除
    public List<string>? NeDocumentRepositoryIds { get; set; }

    // 比對結果
    public string? OnlineReadiness { get; set; }

    // 線上測驗上下架狀態
    public string? ProductStatus { get; set; }
    // public List<ContentSection>? ContentSections { get; set; }

    // 版權
    public string? Copyright { get; set; }

    // 答案格式使用方程式
    public bool? HasLatex { get; set; }

    // 素養題
    public bool? IsLiteracy { get; set; }

    // 題組
    public bool? IsSet { get; set; }

    // 解析(有無解析)
    public bool? HasSolution { get; set; }

    // 解題影片
    public bool? HasVideoUrls { get; set; }
}