
# itembank-index-backend
that introduction will divide into two parts:
* `Elasticsearch Client (this APP)`
* `Elastisearch Daemon (elasticsearch engine instance)`

## Elasticsearch Client

### Docker
* Build
  ```shell
  docker build -t itembank-index-backend .
  ```
* Run
  ```shell
  docker run -d -it -p 5213:5213 --restart always --name itembank-index-backend itembank-index-backend
  ```
  
### appsettings
ElasticsearchReservedWordSeasrchEnable: enabled the reserved word replacement that needs to restart the service and migrate all documents again.
<br>NOTICE: it must not be used with the index that its configuration already involves the **char_filter.special_chars** at the same time.

## Elasticsearch Daemon

### Cloud compute engine construction step by step
* Update Package Index:
  ```shell
  sudo apt update
  ```
* Install Prerequisites:
  ```shell
  sudo apt install apt-transport-https ca-certificates curl software-properties-common
  ```
* Add Docker’s Official GPG Key:
  ```shell
  curl -fsSL https://download.docker.com/linux/debian/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
  ```
* Add Docker APT Repository:
  ```shell
  echo "deb [arch=amd64 signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
  ```
* Update Package Index Again:
  ```shell
  sudo apt update
  ```
* Install Docker CE:
  ```shell
  sudo apt install docker-ce
  ```
* Verify Docker Installation:
  ```shell
  sudo systemctl status docker
  docker --version
  ```
* Pull the Elasticsearch Docker image:
  ```shell
  docker pull docker.elastic.co/elasticsearch/elasticsearch:8.14.3
  ```
* Create a new docker network:
  ```shell
  docker network create elastic
  ```
* Create a volume:
  ```shell
  docker volume create esdata
  docker volume create esconfig
  ```
* Start an Elasticsearch container with name **"es01"**:
  ```shell
  docker run --name es01 --net elastic -p 9200:9200 -p 9300:9300 \
  -d -it -m 10GB --restart always \
  -v /mnt/elasticsearch/esdata/_data:/usr/share/elasticsearch/data \
  -v /mnt/elasticsearch/esconfig/_data:/usr/share/elasticsearch/config \
  -e "discovery.type=single-node" \
  docker.elastic.co/elasticsearch/elasticsearch:8.14.3
  ```
* If this error occurs: max virtual memory areas vm.max_map_count [65530] is too low, increase to at least [262144]
  ```shell
  # vim /etc/sysctl.conf
  vm.max_map_count=655360 // add this line
  # sysctl -p
  >> vm.max_map_count = 655360 // expected output
  ```
* API Key generation: you can use curl or kibana console: Management > Dev Tools page
  * See [Elastic daeomn]() in the below to **create user first** for basic authentication
  * Generate API key by curl
  ```shell
  curl -X POST "http://USERNAME:PASSWORD@localhost:9200/_security/api_key" -H "Content-Type: application/json" -d'
  {
    "name": "itembank-elastic-api-key",
    "role_descriptors": {
      "my_custom_role": {
        "cluster": ["all"],
        "index": [
          {
            "names": ["*"],
            "privileges": ["all"]
          }
        ]
      }
    }
  }'
  ```
  * Response, will provide api key information when successful
  ```shell
  {
    "id": "VuaCfGcBCdbkQm-e5aOx",
    "name": "my-api-key",
    "api_key": "ui2lp2axTNmsyakw9tvNnw",
    "encoded": "VnVhQ2ZHY0JDZGJrUW0tZTVhT3g6dWkybHAyYXhUTm1zeWFrdzl0dk5udw=="  
  }
  ```
  * Set the api key and id as environment variable from copying the response info
  ```shell
  ELASTICSEARCH_API_KEY_ID=VuaCfGcBCdbkQm-e5aOx
  ELASTICSEARCH_API_KEY=VnVhQ2ZHY0JDZGJrUW0tZTVhT3g6dWkybHAyYXhUTm1zeWFrdzl0dk5udw==
  ```

---
### Elastic daemon
* Create user first for basic authentication
  ```shell
  bin/elasticsearch-users useradd USERNAME -p PASSWORD -r superuser
  ```
* Install plugin and restart service to enable for analyzer, the version should match elastic server then restart after installed
    ```shell
    bin/elasticsearch-plugin install https://get.infini.cloud/elasticsearch/analysis-ik/8.14.3
    bin/elasticsearch-plugin install https://get.infini.cloud/elasticsearch/analysis-stconvert/8.14.3
    # exit to VM terminal and restart container
    sudo docker restart es01     
    ```

---
### Elastic VM (GCE)
* Create item index: itembank-production and itembank-development
* NOTICE: There are two types of index below, one of the two has used the custom analyzer using the configuration of **char_filter.special_chars**
to mapping the reserved words and you can set the number of shards to 2 in the production index.
* Index with custom analyzer
    ```shell
    curl -L -X PUT 'localhost:9200/itembank-production' \
    -H 'Content-Type: application/json' \
    -H 'Authorization: ApiKey $ELASTICSEARCH_API_KEY' \
    -d '    {
      "settings": {
        "index": {
          "number_of_shards": "1",
          "max_result_window": "2147483647"
        },
        "analysis": {
          "char_filter": {
            "special_chars": {
              "type": "mapping",
              "mappings": [
                "+ => _PLUS_",
                "＋ => _PLUS_",
                "- => _HYPHEN_",
                "－ => _HYPHEN_",
                "= => _EQUALS_",
                "＝ => _EQUALS_",
                "&& => _LOGICAL_AND_",
                "＆＆ => _LOGICAL_AND_",
                "|| => _LOGICAL_OR_",
                "｜｜ => _LOGICAL_OR_",
                "> => _GREATER_THAN_",
                "＞ => _GREATER_THAN_",
                "< => _LESS_THAN_",
                "＜ => _LESS_THAN_",
                "! => _EXCLAMATION_",
                "！ => _EXCLAMATION_",
                "( => _LEFT_PARENTHESIS_",
                "（ => _LEFT_PARENTHESIS_",
                ") => _RIGHT_PARENTHESIS_",
                "） => _RIGHT_PARENTHESIS_",
                "{ => _LEFT_BRACE_",
                "｛ => _LEFT_BRACE_",
                "} => _RIGHT_BRACE_",
                "｝ => _RIGHT_BRACE_",
                "[ => _LEFT_BRACKET_",
                "［ => _LEFT_BRACKET_",
                "] => _RIGHT_BRACKET_",
                "］ => _RIGHT_BRACKET_",
                "^ => _CARET_",
                "＾ => _CARET_",
                "\" => _QUOTATION_MARK_",
                "” => _QUOTATION_MARK_",
                "“ => _QUOTATION_MARK_",
                "~ => _TILDE_",
                "～ => _TILDE_",
                "* => _ASTERISK_",
                "＊ => _ASTERISK_",
                "? => _QUESTION_MARK_",
                "？ => _QUESTION_MARK_",
                ": => _COLON_",
                "： => _COLON_",
                "\\\\ => _BACK_SLASH_",
                "＼ => _BACK_SLASH_",
                "/ => _SLASH_",
                "／ => _SLASH_"
              ]
            }
          },
          "analyzer": {
            "my_analyzer": {
              "type": "custom",
              "char_filter": [
                "special_chars"
              ],
              "tokenizer": "standard"
            }
          }
        }
      },
      "mappings": {
        "properties": {
          "abilityCodes": {
            "type": "keyword"
          },
          "abilityIds": {
            "type": "keyword"
          },
          "bodyOfKnowledgeCodes": {
            "type": "keyword"
          },
          "bodyOfKnowledgeIds": {
            "type": "keyword"
          },
          "catalogIds": {
            "type": "keyword"
          },
          "catalogTags": {
            "type": "keyword"
          },
          "catalogs": {
            "type": "nested",
            "properties": {
              "id": {
                "type": "keyword"
              },
              "isShared": {
                "type": "keyword"
              },
              "sources": {
                "type": "keyword"
              },
              "userTypes": {
                "type": "keyword"
              }
            }
          },
          "contentSectionCodes": {
            "type": "keyword"
          },
          "contentSectionIds": {
            "type": "keyword"
          },
          "contentSections": {
            "type": "nested",
            "properties": {
              "code": {
                "type": "keyword"
              },
              "contentId": {
                "type": "keyword"
              },
              "id": {
                "type": "keyword"
              },
              "subjectId": {
                "type": "keyword"
              },
              "version": {
                "type": "keyword"
              },
              "volume": {
                "type": "keyword"
              },
              "year": {
                "type": "keyword"
              }
            }
          },
          "correctness": {
            "type": "keyword"
          },
          "createdOn": {
            "type": "date"
          },
          "documentIds": {
            "type": "keyword"
          },
          "documentRepositoryIds": {
            "type": "keyword"
          },
          "editorRemark": {
            "type": "text",
            "analyzer": "my_analyzer"
          },
          "fileNames": {
            "type": "keyword"
          },
          "hasCopyright": {
            "type": "boolean"
          },
          "hasImportRemark": {
            "type": "boolean"
          },
          "hasVideoUrls": {
            "type": "boolean"
          },
          "id": {
            "type": "keyword"
          },
          "identifier": {
            "type": "keyword"
          },
          "importRecordIds": {
            "type": "keyword"
          },
          "isCorrect": {
            "type": "boolean"
          },
          "isLiteracy": {
            "type": "boolean"
          },
          "isSet": {
            "type": "boolean"
          },
          "itemYears": {
            "type": "nested",
            "properties": {
              "bodyOfKnowledgeCode": {
                "type": "keyword"
              },
              "dimensionValueIds": {
                "type": "keyword"
              },
              "usageType": {
                "type": "keyword"
              },
              "year": {
                "type": "keyword"
              }
            }
          },
          "knowledgeCodes": {
            "type": "keyword"
          },
          "knowledgeIds": {
            "type": "keyword"
          },
          "labelNames": {
            "type": "keyword"
          },
          "lessonCodes": {
            "type": "keyword"
          },
          "lessonIds": {
            "type": "keyword"
          },
          "onlineReadiness": {
            "type": "keyword"
          },
          "optionAbilityCodes": {
            "type": "keyword"
          },
          "optionAbilityIds": {
            "type": "keyword"
          },
          "optionKnowledgeCodes": {
            "type": "keyword"
          },
          "optionKnowledgeIds": {
            "type": "keyword"
          },
          "optionLessonCodes": {
            "type": "keyword"
          },
          "optionLessonIds": {
            "type": "keyword"
          },
          "optionRecognitionCodes": {
            "type": "keyword"
          },
          "optionRecognitionIds": {
            "type": "keyword"
          },
          "preamble": {
            "type": "text",
            "analyzer": "my_analyzer"
          },
          "preambleAbilityCodes": {
            "type": "keyword"
          },
          "preambleAbilityIds": {
            "type": "keyword"
          },
          "preambleKnowledgeCodes": {
            "type": "keyword"
          },
          "preambleKnowledgeIds": {
            "type": "keyword"
          },
          "preambleLessonCodes": {
            "type": "keyword"
          },
          "preambleLessonIds": {
            "type": "keyword"
          },
          "preambleRecognitionCodes": {
            "type": "keyword"
          },
          "preambleRecognitionIds": {
            "type": "keyword"
          },
          "productCodes": {
            "type": "keyword"
          },
          "productStatuses": {
            "type": "nested",
            "properties": {
              "comment": {
                "type": "text",
                "analyzer": "my_analyzer"
              },
              "status": {
                "type": "keyword"
              },
              "target": {
                "type": "keyword"
              }
            }
          },
          "products": {
            "type": "nested",
            "properties": {
              "code": {
                "type": "keyword"
              },
              "id": {
                "type": "keyword"
              },
              "isShared": {
                "type": "keyword"
              },
              "sources": {
                "type": "keyword"
              },
              "userTypes": {
                "type": "keyword"
              },
              "year": {
                "type": "keyword"
              }
            }
          },
          "publishSources": {
            "type": "keyword"
          },
          "questionAbilityCodes": {
            "type": "keyword"
          },
          "questionAbilityIds": {
            "type": "keyword"
          },
          "questionKnowledgeCodes": {
            "type": "keyword"
          },
          "questionKnowledgeIds": {
            "type": "keyword"
          },
          "questionLessonCodes": {
            "type": "keyword"
          },
          "questionLessonIds": {
            "type": "keyword"
          },
          "questionRecognitionCodes": {
            "type": "keyword"
          },
          "questionRecognitionIds": {
            "type": "keyword"
          },
          "questions": {
            "type": "nested",
            "properties": {
              "answerKeywords": {
                "type": "text",
                "analyzer": "my_analyzer"
              },
              "answeringMethod": {
                "type": "keyword"
              },
              "answers": {
                "type": "keyword"
              },
              "latexAnswers": {
                "type": "boolean"
              },
              "options": {
                "type": "text",
                "analyzer": "my_analyzer"
              },
              "proposeAnswers": {
                "type": "keyword"
              },
              "stem": {
                "type": "text",
                "analyzer": "my_analyzer"
              }
            }
          },
          "recognitionCodes": {
            "type": "keyword"
          },
          "recognitionIds": {
            "type": "keyword"
          },
          "solution": {
            "type": "text",
            "analyzer": "my_analyzer"
          },
          "sources": {
            "type": "keyword"
          },
          "topic": {
            "type": "keyword"
          },
          "updatedOn": {
            "type": "date"
          },
          "userType": {
            "type": "keyword"
          },
          "userTypes": {
            "type": "keyword"
          },
          "versionIds": {
            "type": "keyword"
          },
          "volumeNames": {
            "type": "keyword"
          }
        }
      }
    }'
    ```
* Index with default analyzer
    ```shell
    curl -L -X PUT 'localhost:9200/itembank-production' \
    -H 'Content-Type: application/json' \
    -H 'Authorization: ApiKey $ELASTICSEARCH_API_KEY' \
    -d '{
      "settings": {
        "index": {
          "number_of_shards": "1",
          "max_result_window": "2147483647"
        },
        "analysis": {
          "analyzer": {
            "default": {
              "type": "standard"
            }
          }
        }
      },
      "mappings": {
        "properties": {
          "abilityCodes": {
            "type": "keyword"
          },
          "abilityIds": {
            "type": "keyword"
          },
          "subjectIds": {
            "type": "keyword"
          },
          "bodyOfKnowledgeCodes": {
            "type": "keyword"
          },
          "bodyOfKnowledgeIds": {
            "type": "keyword"
          },
          "catalogIds": {
            "type": "keyword"
          },
          "catalogTags": {
            "type": "keyword"
          },
          "catalogs": {
            "type": "nested",
            "properties": {
              "id": {
                "type": "keyword"
              },
              "isShared": {
                "type": "keyword"
              },
              "sources": {
                "type": "keyword"
              },
              "userTypes": {
                "type": "keyword"
              }
            }
          },
          "contentSectionCodes": {
            "type": "keyword"
          },
          "contentSectionIds": {
            "type": "keyword"
          },
          "contentSections": {
            "type": "nested",
            "properties": {
              "code": {
                "type": "keyword"
              },
              "contentId": {
                "type": "keyword"
              },
              "id": {
                "type": "keyword"
              },
              "subjectId": {
                "type": "keyword"
              },
              "version": {
                "type": "keyword"
              },
              "volume": {
                "type": "keyword"
              },
              "year": {
                "type": "keyword"
              }
            }
          },
          "correctness": {
            "type": "keyword"
          },
          "createdOn": {
            "type": "date"
          },
          "documentIds": {
            "type": "keyword"
          },
          "documentRepositoryIds": {
            "type": "keyword"
          },
          "editorRemark": {
            "type": "text"
          },
          "fileNames": {
            "type": "keyword"
          },
          "copyright": {
            "type": "keyword"
          },
          "hasImportRemark": {
            "type": "boolean"
          },
          "hasVideoUrls": {
            "type": "boolean"
          },
          "id": {
            "type": "keyword"
          },
          "identifier": {
            "type": "keyword"
          },
          "importRecordIds": {
            "type": "keyword"
          },
          "isCorrect": {
            "type": "boolean"
          },
          "isLiteracy": {
            "type": "boolean"
          },
          "isSet": {
            "type": "boolean"
          },
          "itemYears": {
            "type": "nested",
            "properties": {
              "bodyOfKnowledgeCode": {
                "type": "keyword"
              },
              "dimensionValueIds": {
                "type": "keyword"
              },
              "usageType": {
                "type": "keyword"
              },
              "year": {
                "type": "keyword"
              }
            }
          },
          "regularKnowledgeCodes": {
            "type": "keyword"
          },
          "regularKnowledgeIds": {
            "type": "keyword"
          },
          "discreteKnowledgeCodes": {
            "type": "keyword"
          },
          "discreteKnowledgeIds": {
            "type": "keyword"
          },
          "labelNames": {
            "type": "keyword"
          },
          "regularLessonCodes": {
            "type": "keyword"
          },
          "regularLessonIds": {
            "type": "keyword"
          },
          "discreteLessonCodes": {
            "type": "keyword"
          },
          "discreteLessonIds": {
            "type": "keyword"
          },
          "onlineReadiness": {
            "type": "keyword"
          },
          "optionAbilityCodes": {
            "type": "keyword"
          },
          "optionAbilityIds": {
            "type": "keyword"
          },
          "optionKnowledgeCodes": {
            "type": "keyword"
          },
          "optionKnowledgeIds": {
            "type": "keyword"
          },
          "optionLessonCodes": {
            "type": "keyword"
          },
          "optionLessonIds": {
            "type": "keyword"
          },
          "optionRecognitionCodes": {
            "type": "keyword"
          },
          "optionRecognitionIds": {
            "type": "keyword"
          },
          "preamble": {
            "type": "text"
          },
          "preambleAbilityCodes": {
            "type": "keyword"
          },
          "preambleAbilityIds": {
            "type": "keyword"
          },
          "preambleKnowledgeCodes": {
            "type": "keyword"
          },
          "preambleKnowledgeIds": {
            "type": "keyword"
          },
          "preambleLessonCodes": {
            "type": "keyword"
          },
          "preambleLessonIds": {
            "type": "keyword"
          },
          "preambleRecognitionCodes": {
            "type": "keyword"
          },
          "preambleRecognitionIds": {
            "type": "keyword"
          },
          "productCodes": {
            "type": "keyword"
          },
          "productStatuses": {
            "type": "nested",
            "properties": {
              "comment": {
                "type": "text"
              },
              "status": {
                "type": "keyword"
              },
              "target": {
                "type": "keyword"
              }
            }
          },
          "products": {
            "type": "nested",
            "properties": {
              "code": {
                "type": "keyword"
              },
              "id": {
                "type": "keyword"
              },
              "isShared": {
                "type": "keyword"
              },
              "sources": {
                "type": "keyword"
              },
              "userTypes": {
                "type": "keyword"
              },
              "year": {
                "type": "keyword"
              }
            }
          },
          "publishSources": {
            "type": "keyword"
          },
          "questionAbilityCodes": {
            "type": "keyword"
          },
          "questionAbilityIds": {
            "type": "keyword"
          },
          "questionKnowledgeCodes": {
            "type": "keyword"
          },
          "questionKnowledgeIds": {
            "type": "keyword"
          },
          "questionLessonCodes": {
            "type": "keyword"
          },
          "questionLessonIds": {
            "type": "keyword"
          },
          "questionRecognitionCodes": {
            "type": "keyword"
          },
          "questionRecognitionIds": {
            "type": "keyword"
          },
          "questions": {
            "type": "nested",
            "properties": {
              "answerKeywords": {
                "type": "text"
              },
              "answeringMethod": {
                "type": "keyword"
              },
              "answers": {
                "type": "keyword"
              },
              "latexAnswers": {
                "type": "boolean"
              },
              "options": {
                "type": "text"
              },
              "proposeAnswers": {
                "type": "keyword"
              },
              "stem": {
                "type": "text"
              }
            }
          },
          "recognitionCodes": {
            "type": "keyword"
          },
          "recognitionIds": {
            "type": "keyword"
          },
          "solution": {
            "type": "text"
          },
          "sources": {
            "type": "keyword"
          },
          "topic": {
            "type": "keyword"
          },
          "updatedOn": {
            "type": "date"
          },
          "userType": {
            "type": "keyword"
          },
          "userTypes": {
            "type": "keyword"
          },
          "versionIds": {
            "type": "keyword"
          },
          "volumeNames": {
            "type": "keyword"
          }
        }
      }
    }'
    ```
* [optional] Create TTL policy for record (searchId)

  suggest to use built-in policies `7-days@lifecycle` or `30-days@lifecycle`..., etc, you can get all policies by the command that explained in the next step
    ```shell
    curl -L -X PUT 'localhost:9200/_ilm/policy/policy_7d' \
    -H 'Content-Type: application/json' \
    -H 'Authorization: ApiKey $ELASTICSEARCH_API_KEY' \
    -d '{
      "policy": {
        "phases": {
          "hot": {
            "actions": {
              "rollover": {
                "max_age": "1d"
              }
            }
          },
          "delete": {
            "min_age": "7d",
            "actions": {
              "delete": {}
            }
          }
        }
      }
    }'
    ```
* Get all policies
    ```shell
    curl -L 'localhost:9200/_ilm/policy' \
    -H 'Content-Type: application/json' \
    -H 'Authorization: ApiKey $ELASTICSEARCH_API_KEY'
    ```
* Create `record` index (searchId): itembank-production and itembank-development
    ```shell
    curl -L -X PUT 'localhost:9200/itembank-production-search-record-000001' \
    -H 'Content-Type: application/json' \
    -H 'Authorization: ApiKey $ELASTICSEARCH_API_KEY' \
    -d '{
      "settings": {
        "index.lifecycle.name": "30-days@lifecycle",
        "index.lifecycle.rollover_alias": "itembank-production-search-record",
        "max_result_window": "2147483647",
      },
      "aliases": {
        "itembank-production-search-record": {
          "is_write_index": true
        }
      },
      "mappings": {
        "properties": {
          "data": {
            "type": "text"
          }
        }
      }
    }'
    ```
  * Update index settings if needed
    ```shell
    curl -L -X PUT 'localhost:9200/itembank-production-search-record-000001/_settings' \
    -H 'Content-Type: application/json' \
    -H 'Authorization: ApiKey $ELASTICSEARCH_API_KEY' \
    -d '{
      "settings": {
      "index.lifecycle.name": "180-days@lifecycle"
      }
    }'
    ```
