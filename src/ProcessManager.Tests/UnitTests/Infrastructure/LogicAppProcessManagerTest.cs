using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using ProcessManager.Domain.Interfaces;
using ProcessManager.Domain.Models;
using ProcessManager.Infrastructure.Services;
using Xunit;

namespace ProcessManager.Tests.UnitTests.Infrastructure
{
    public class LogicAppProcessManagerTest
    {
        private readonly Mock<ICacheRepository> mockCache = new Mock<ICacheRepository>();

        [Fact]
        public async Task GetProcessWithMessage_NullOrEmptyKey_Throws()
        {
            // ARRANGE
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.BadGateway, "error message");
            var mockConfig = new Mock<IConfigurationService>();

            var subjectUnderTest = new LogicAppProcessService("", "", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);

            // ACT
            // ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => subjectUnderTest
               .GetProcessWithMessageAsync("", "123", "key", null));
        }

        [Fact]
        public async Task GetProcessWithMessage_Null_Throws()
        {
            // ARRANGE
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.BadGateway, "error message");
            var mockConfig = new Mock<IConfigurationService>();

            var subjectUnderTest = new LogicAppProcessService("", "", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);

            // ACT
            // ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => subjectUnderTest
               .GetProcessWithMessageAsync("123", null, null, null));
        }

        [Fact]
        public async Task GetProcessWithMessage_NullMessage_Throws()
        {
            // ARRANGE
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.BadGateway, "error message");
            var mockConfig = new Mock<IConfigurationService>();

            var subjectUnderTest = new LogicAppProcessService("", "", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);

            // ACT
            // ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => subjectUnderTest
               .GetProcessWithMessageAsync("123", "Process123", null, null));
        }

        [Fact]
        public async Task GetPrincipalId_Null_Throws()
        {
            // ARRANGE
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.BadGateway, "error message");
            var mockConfig = new Mock<IConfigurationService>();

            var subjectUnderTest = new LogicAppProcessService("", "", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);

            // ACT
            // ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => subjectUnderTest
                .GetPrincipalIdAsync(null, null));
        }

        [Fact]
        public async Task GetProcessWithMessage_Azure_Unavailable_Throws()
        {
            // ARRANGE
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.BadGateway, "error message");
            var mockConfig = new Mock<IConfigurationService>();
            mockCache.Setup(cache => cache.GetOrSetValueAsync<string>(It.IsAny<Func<Task<string>>>(), It.IsAny<string>(), null, null, default)).Throws(new HttpRequestException());
            var subjectUnderTest = new LogicAppProcessService("", "", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);

            // ACT
            // ASSERT
            await Assert.ThrowsAsync<HttpRequestException>(() => subjectUnderTest
               .GetProcessWithMessageAsync("123", "key", new { Test = "test" }, null));
        }

        [Fact]
        public async Task GetPrincipalId_Azure_Unavailable_Throws()
        {
            // ARRANGE
            mockCache.Setup(cr => cr.GetOrSetValueAsync(It.IsAny<Func<Task<Guid>>>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException());
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.BadGateway, "error message");
            var mockConfig = new Mock<IConfigurationService>();

            var subjectUnderTest = new LogicAppProcessService("", "", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);

            // ACT
            // ASSERT
            await Assert.ThrowsAsync<HttpRequestException>(() => subjectUnderTest
                .GetPrincipalIdAsync("123", null));
        }

        [Fact]
        public async Task GetProcessWithMessage_KeyExists_Succeedes()
        {
            string azureResponse = "{ \"value\": \"http://test.com/\" }";
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.OK, azureResponse);
            var mockConfig = new Mock<IConfigurationService>();
            mockCache.Setup(cache => cache.GetOrSetValueAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<string>(), null, null, default))
                .ReturnsAsync("http://test.com/");

            var subjectUnderTest = new LogicAppProcessService("", "", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);

            // ACT
            var process = await subjectUnderTest
               .GetProcessWithMessageAsync("123", "key", new { Test = "test" }, null);

            // ASSERT
            mockCache.Verify();

            Assert.Equal("http://test.com/", process.StartUrl);
        }

        [Fact]
        public async Task GetProcessWithMessage_KeyExists_WithEnvironmentName_Succeeded()
        {
            string azureResponse = "{ \"value\": \"http://test.com/\" }";
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.OK, azureResponse);
            var mockConfig = new Mock<IConfigurationService>();
            mockCache.Setup(cache => cache.GetOrSetValueAsync<string>(It.IsAny<Func<Task<string>>>(), It.IsAny<string>(), null, null, default)).ReturnsAsync("http://test.com/");


            var subjectUnderTest = new LogicAppProcessService("", "devpfm-k3y-sbx-rg", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);

            // ACT
            var process = await subjectUnderTest
                .GetProcessWithMessageAsync("123", "key", new { Test = "test" }, "stg");

            // ASSERT
            Assert.Equal("http://test.com/", process.StartUrl);
        }

        [Fact]
        public async Task GetProcessWithMessage_KeyExists_PlatformLevel_WithEnvironmentName_Succeeded()
        {
            string azureResponse = "{ \"value\": \"http://test.com/\" }";
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.OK, azureResponse);
            var mockConfig = new Mock<IConfigurationService>();
            mockCache.Setup(cache => cache.GetOrSetValueAsync(It.IsAny<Func<Task<string>>>(), It.IsAny<string>(), null, null, default))
                .ReturnsAsync("http://test.com/");
            var subjectUnderTest = new LogicAppProcessService("", "devpfm-platform-rg", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);


            // ACT
            var process = await subjectUnderTest
                .GetProcessWithMessageAsync("123", "key", new { Test = "test" }, "stg");

            // ASSERT
            Assert.Equal("http://test.com/", process.StartUrl);
        }

        [Fact]
        public async Task GetPrincipalId_KeyExists_Succeedes()
        {
            string azureResponse = "{\r\n  \"properties\": {\r\n    \"provisioningState\": \"Succeeded\",\r\n    \"createdTime\": \"2020-09-10T14:35:01.816047Z\",\r\n    \"changedTime\": \"2021-05-27T13:49:55.0519601Z\",\r\n    \"state\": \"Enabled\",\r\n    \"version\": \"08585794834904292605\",\r\n    \"accessEndpoint\": \"https://prod-166.westeurope.logic.azure.com:443/workflows/5f854267318d493385dcea77f27b2cd5\",\r\n    \"definition\": {\r\n      \"$schema\": \"https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#\",\r\n      \"contentVersion\": \"1.0.0.0\",\r\n      \"parameters\": {\r\n        \"$connections\": {\r\n          \"defaultValue\": {},\r\n          \"type\": \"Object\"\r\n        },\r\n        \"GuardianProductRoleId\": {\r\n          \"defaultValue\": \"09ecacd2-1635-4859-affb-421b289b6e0f\",\r\n          \"type\": \"String\"\r\n        },\r\n        \"GuardianRelationType\": {\r\n          \"defaultValue\": \"275b6e62-08b1-4ee5-a827-7b7a3191d31c\",\r\n          \"type\": \"String\"\r\n        },\r\n        \"HTTP_Get_Loan_Data-URI\": {\r\n          \"defaultValue\": \"https://apim.orc.my.fivedegrees.cloud/loanintegrationapi-sbx/api/loans\",\r\n          \"type\": \"String\"\r\n        },\r\n        \"final_statuses\": {\r\n          \"defaultValue\": \"PAID UP,WRITE OFF,DELIVERED\",\r\n          \"type\": \"String\"\r\n        }\r\n      },\r\n      \"triggers\": {\r\n        \"manual\": {\r\n          \"type\": \"Request\",\r\n          \"kind\": \"Http\",\r\n          \"inputs\": {\r\n            \"schema\": {\r\n              \"properties\": {\r\n                \"assignToId\": {\r\n                  \"type\": \"string\"\r\n                },\r\n                \"fourEyesPrinciple\": {\r\n                  \"type\": \"boolean\"\r\n                },\r\n                \"manualTask\": {\r\n                  \"type\": \"boolean\"\r\n                },\r\n                \"manualTaskName\": {\r\n                  \"type\": \"string\"\r\n                },\r\n                \"parameters\": {\r\n                  \"properties\": {\r\n                    \"createdBy\": {\r\n                      \"type\": \"string\"\r\n                    },\r\n                    \"externalId\": {\r\n                      \"type\": \"string\"\r\n                    },\r\n                    \"operationId\": {\r\n                      \"type\": \"string\"\r\n                    },\r\n                    \"startMessage\": {\r\n                      \"properties\": {\r\n                        \"correlationId\": {\r\n                          \"type\": \"string\"\r\n                        },\r\n                        \"entityA\": {\r\n                          \"type\": \"string\"\r\n                        },\r\n                        \"entityB\": {\r\n                          \"type\": \"string\"\r\n                        },\r\n                        \"fromDate\": {\r\n                          \"type\": \"string\"\r\n                        },\r\n                        \"isGuardianForAllProducts\": {\r\n                          \"type\": \"boolean\"\r\n                        },\r\n                        \"toDate\": {}\r\n                      },\r\n                      \"type\": \"object\"\r\n                    }\r\n                  },\r\n                  \"type\": \"object\"\r\n                }\r\n              },\r\n              \"type\": \"object\"\r\n            }\r\n          }\r\n        }\r\n      },\r\n      \"actions\": {\r\n        \"Initialize_ActiveProductRelations_variable\": {\r\n          \"runAfter\": {\r\n            \"Initialize_CommandIds_Variable\": [\r\n              \"Succeeded\"\r\n            ]\r\n          },\r\n          \"type\": \"InitializeVariable\",\r\n          \"inputs\": {\r\n            \"variables\": [\r\n              {\r\n                \"name\": \"ActiveProductRelations\",\r\n                \"type\": \"array\",\r\n                \"value\": []\r\n              }\r\n            ]\r\n          }\r\n        },\r\n        \"Initialize_CommandId_Variable\": {\r\n          \"runAfter\": {\r\n            \"Initialize_IsGuardian_variable\": [\r\n              \"Succeeded\"\r\n            ]\r\n          },\r\n          \"type\": \"InitializeVariable\",\r\n          \"inputs\": {\r\n            \"variables\": [\r\n              {\r\n                \"name\": \"CommandId\",\r\n                \"type\": \"string\"\r\n              }\r\n            ]\r\n          }\r\n        },\r\n        \"Initialize_CommandIds_Variable\": {\r\n          \"runAfter\": {\r\n            \"Initialize_CommandId_Variable\": [\r\n              \"Succeeded\"\r\n            ]\r\n          },\r\n          \"type\": \"InitializeVariable\",\r\n          \"inputs\": {\r\n            \"variables\": [\r\n              {\r\n                \"name\": \"CommandIds\",\r\n                \"type\": \"array\",\r\n                \"value\": []\r\n              }\r\n            ]\r\n          }\r\n        },\r\n        \"Initialize_FourEyesSubjectId_Variable\": {\r\n          \"runAfter\": {},\r\n          \"type\": \"InitializeVariable\",\r\n          \"inputs\": {\r\n            \"variables\": [\r\n              {\r\n                \"name\": \"FourEyesSubjectId\",\r\n                \"type\": \"string\",\r\n                \"value\": \"@{if(equals(triggerBody()?['fourEyesPrinciple'], true), triggerBody()?['parameters']?['createdBy'], '00000000-0000-0000-0000-000000000000')}\"\r\n              }\r\n            ]\r\n          }\r\n        },\r\n        \"Initialize_IsGuardian_variable\": {\r\n          \"runAfter\": {\r\n            \"Initialize_FourEyesSubjectId_Variable\": [\r\n              \"Succeeded\"\r\n            ]\r\n          },\r\n          \"type\": \"InitializeVariable\",\r\n          \"inputs\": {\r\n            \"variables\": [\r\n              {\r\n                \"name\": \"IsGuardian\",\r\n                \"type\": \"boolean\",\r\n                \"value\": \"@triggerBody()?['parameters']?['startMessage']?['isGuardianForAllProducts']\"\r\n              }\r\n            ]\r\n          }\r\n        },\r\n        \"Scope_-_Create_Guardian\": {\r\n          \"actions\": {\r\n            \"Check_If_Guardian_Relation_Is_Created\": {\r\n              \"actions\": {},\r\n              \"runAfter\": {\r\n                \"Parse_Webhook_Response\": [\r\n                  \"Succeeded\"\r\n                ]\r\n              },\r\n              \"else\": {\r\n                \"actions\": {\r\n                  \"Throw_exception_-_Because_Guardian-Relation_Was_Not_Created\": {\r\n                    \"runAfter\": {},\r\n                    \"type\": \"Compose\",\r\n                    \"inputs\": \"@int('ERROR')\"\r\n                  }\r\n                }\r\n              },\r\n              \"expression\": {\r\n                \"and\": [\r\n                  {\r\n                    \"contains\": [\r\n                      \"@body('Parse_Webhook_Response')?[0]?['Type']\",\r\n                      \"EntityRelationCreated\"\r\n                    ]\r\n                  }\r\n                ]\r\n              },\r\n              \"type\": \"If\"\r\n            },\r\n            \"Check_If_Manual_Task_Is_Required\": {\r\n              \"actions\": {\r\n                \"Manual_task_Approved\": {\r\n                  \"actions\": {},\r\n                  \"runAfter\": {\r\n                    \"Parse_Task_Response\": [\r\n                      \"Succeeded\"\r\n                    ]\r\n                  },\r\n                  \"else\": {\r\n                    \"actions\": {\r\n                      \"Publish_CreateGuardianRelationRejected_Event\": {\r\n                        \"runAfter\": {},\r\n                        \"type\": \"ApiConnection\",\r\n                        \"inputs\": {\r\n                          \"body\": [\r\n                            {\r\n                              \"data\": \"@triggerBody()?['parameters']?['startMessage']\",\r\n                              \"eventType\": \"CreateGuardianRelationRejected\",\r\n                              \"id\": \"@triggerBody()?['parameters']?['createdBy']\",\r\n                              \"subject\": \"api/entityrelations/@{triggerBody()?['parameters']?['startMessage']?['entityA']}\",\r\n                              \"topic\": \"entityrelations\"\r\n                            }\r\n                          ],\r\n                          \"host\": {\r\n                            \"connection\": {\r\n                              \"name\": \"@parameters('$connections')['enveventgrid']['connectionId']\"\r\n                            }\r\n                          },\r\n                          \"method\": \"post\",\r\n                          \"path\": \"/eventGrid/api/events\"\r\n                        }\r\n                      },\r\n                      \"Throw_exception_-_Because_Manual_Task_Was_Rejected\": {\r\n                        \"runAfter\": {\r\n                          \"Publish_CreateGuardianRelationRejected_Event\": [\r\n                            \"Succeeded\"\r\n                          ]\r\n                        },\r\n                        \"type\": \"Compose\",\r\n                        \"inputs\": \"@int('ERROR')\"\r\n                      }\r\n                    }\r\n                  },\r\n                  \"expression\": {\r\n                    \"and\": [\r\n                      {\r\n                        \"equals\": [\r\n                          \"@toLower(body('Parse_Task_Response')?['status'])\",\r\n                          \"approved\"\r\n                        ]\r\n                      }\r\n                    ]\r\n                  },\r\n                  \"type\": \"If\"\r\n                },\r\n                \"Parse_Task_Response\": {\r\n                  \"runAfter\": {\r\n                    \"Task\": [\r\n                      \"Succeeded\"\r\n                    ]\r\n                  },\r\n                  \"type\": \"ParseJson\",\r\n                  \"inputs\": {\r\n                    \"content\": \"@body('Task')\",\r\n                    \"schema\": {\r\n                      \"properties\": {\r\n                        \"status\": {\r\n                          \"type\": \"string\"\r\n                        }\r\n                      },\r\n                      \"type\": \"object\"\r\n                    }\r\n                  }\r\n                },\r\n                \"Task\": {\r\n                  \"runAfter\": {},\r\n                  \"type\": \"Workflow\",\r\n                  \"inputs\": {\r\n                    \"body\": {\r\n                      \"assignedToEntityId\": \"@triggerBody()?['assignToId']\",\r\n                      \"assignmentType\": \"UserGroup\",\r\n                      \"correlationId\": \"@triggerBody()?['parameters']?['startMessage']?['correlationId']\",\r\n                      \"createdBy\": \"@triggerBody()?['parameters']?['createdBy']\",\r\n                      \"data\": \"{\\n\\\"entityA\\\": \\\"@{triggerBody()?['parameters']?['startMessage']?['entityA']}\\\",\\n\\\"entityB\\\": \\\"@{triggerBody()?['parameters']?['startMessage']?['entityB']}\\\",\\n\\\"relationType\\\":\\\"@{parameters('GuardianRelationType')}\\\",\\n\\\"correlationId\\\": \\\"@{triggerBody()?['parameters']?['startMessage']?['correlationId']}\\\",\\n\\\"fromDate\\\": \\\"@{triggerBody()?['parameters']?['startMessage']?['fromDate']}\\\",\\n\\\"toDate\\\": \\\"@{triggerBody()?['parameters']?['startMessage']?['toDate']}\\\",\\n\\\"isGuardianForAllProducts\\\": \\\"@{triggerBody()?['parameters']?['startMessage']?['isGuardianForAllProducts']}\\\"\\n}\",\r\n                      \"externalId\": \"@triggerBody()?['parameters']?['externalId']\",\r\n                      \"fourEyeSubjectId\": \"@variables('FourEyesSubjectId')\",\r\n                      \"operationId\": \"@triggerBody()?['parameters']?['operationId']\",\r\n                      \"relations\": [\r\n                        {\r\n                          \"entityId\": \"@triggerBody()?['parameters']?['startMessage']?['entityA']\",\r\n                          \"entityType\": \"Person\"\r\n                        },\r\n                        {\r\n                          \"entityId\": \"@triggerBody()?['parameters']?['startMessage']?['entityB']\",\r\n                          \"entityType\": \"Person\"\r\n                        }\r\n                      ],\r\n                      \"sourceId\": \"@triggerBody()?['parameters']?['startMessage']?['correlationId']\",\r\n                      \"sourceName\": \"Create Guardian\",\r\n                      \"status\": \"Not Started\",\r\n                      \"subject\": \"@triggerBody()?['manualTaskName']\",\r\n                      \"taskType\": 0\r\n                    },\r\n                    \"headers\": {\r\n                      \"x-request-id\": \"@{triggerOutputs()?['headers']?['X-Request-ID']}\"\r\n                    },\r\n                    \"host\": {\r\n                      \"triggerName\": \"manual\",\r\n                      \"workflow\": {\r\n                        \"id\": \"/subscriptions/755f2cc1-0d1c-40da-b6ad-9bbfe83a6d7a/resourceGroups/devpfm-k3y-sbx-rg/providers/Microsoft.Logic/workflows/Task\"\r\n                      }\r\n                    }\r\n                  }\r\n                }\r\n              },\r\n              \"runAfter\": {},\r\n              \"expression\": {\r\n                \"and\": [\r\n                  {\r\n                    \"equals\": [\r\n                      \"@triggerBody()?['manualTask']\",\r\n                      true\r\n                    ]\r\n                  }\r\n                ]\r\n              },\r\n              \"type\": \"If\"\r\n            },\r\n            \"HTTP_Webhook_Subscribe_To_Event_Gateway_For_EntityRelation_Event\": {\r\n              \"runAfter\": {\r\n                \"Check_If_Manual_Task_Is_Required\": [\r\n                  \"Succeeded\"\r\n                ]\r\n              },\r\n              \"limit\": {\r\n                \"timeout\": \"PT330S\"\r\n              },\r\n              \"type\": \"HttpWebhook\",\r\n              \"inputs\": {\r\n                \"subscribe\": {\r\n                  \"body\": {\r\n                    \"callback\": \"@{listCallbackUrl()}\",\r\n                    \"eventIds\": [\r\n                      \"@{triggerBody()?['parameters']?['startMessage']?['correlationId']}\"\r\n                    ]\r\n                  },\r\n                  \"headers\": {\r\n                    \"x-external-id\": \"@triggerBody()?['parameters']?['externalId']\",\r\n                    \"x-request-id\": \"@{triggerOutputs()['headers']?['X-Request-ID']}\",\r\n                    \"x-user-id\": \"@triggerBody()?['parameters']?['createdBy']\",\r\n                    \"Ocp-Apim-Subscription-Key\": \"a73bf2a1-87a8-a9bb-57c9-4e22a422601a\"\r\n                  },\r\n                  \"method\": \"POST\",\r\n                  \"uri\": \"https://apim.orc.my.fivedegrees.cloud/eventgateway-sbx/api/Subscribe\"\r\n                },\r\n                \"unsubscribe\": {}\r\n              }\r\n            },\r\n            \"Is_Guardian_For_All_Products\": {\r\n              \"actions\": {\r\n                \"Check_If_Ward_Has_Products\": {\r\n                  \"actions\": {\r\n                    \"HTTP_Webhook_-_Subscribe_To_Event_Gateway_For_All_PersonProduct_Events\": {\r\n                      \"runAfter\": {\r\n                        \"Iterate_Over_Wards_Product_Relations\": [\r\n                          \"Succeeded\"\r\n                        ]\r\n                      },\r\n                      \"limit\": {\r\n                        \"timeout\": \"PT330S\"\r\n                      },\r\n                      \"type\": \"HttpWebhook\",\r\n                      \"inputs\": {\r\n                        \"subscribe\": {\r\n                          \"body\": {\r\n                            \"callback\": \"@{listCallbackUrl()}\",\r\n                            \"eventIds\": \"@variables('CommandIds')\"\r\n                          },\r\n                          \"headers\": {\r\n                            \"x-external-id\": \"@triggerBody()?['parameters']?['externalId']\",\r\n                            \"x-request-id\": \"@{triggerOutputs()['headers']?['X-Request-ID']}\",\r\n                            \"x-user-id\": \"@triggerBody()?['parameters']?['createdBy']\",\r\n                            \"Ocp-Apim-Subscription-Key\": \"a73bf2a1-87a8-a9bb-57c9-4e22a422601a\"\r\n                          },\r\n                          \"method\": \"POST\",\r\n                          \"uri\": \"https://apim.orc.my.fivedegrees.cloud/eventgateway-sbx/api/Subscribe\"\r\n                        },\r\n                        \"unsubscribe\": {}\r\n                      }\r\n                    },\r\n                    \"Iterate_Over_Active_Product_Relations\": {\r\n                      \"foreach\": \"@variables('ActiveProductRelations')\",\r\n                      \"actions\": {\r\n                        \"Parse_Active_Product_Relation_Item\": {\r\n                          \"runAfter\": {},\r\n                          \"type\": \"ParseJson\",\r\n                          \"inputs\": {\r\n                            \"content\": \"@items('Iterate_Over_Active_Product_Relations')\",\r\n                            \"schema\": {\r\n                              \"properties\": {\r\n                                \"commandId\": {\r\n                                  \"type\": \"string\"\r\n                                },\r\n                                \"productId\": {\r\n                                  \"type\": \"string\"\r\n                                },\r\n                                \"productTypeId\": {\r\n                                  \"type\": \"string\"\r\n                                },\r\n                                \"relationTypeId\": {\r\n                                  \"type\": \"string\"\r\n                                }\r\n                              },\r\n                              \"type\": \"object\"\r\n                            }\r\n                          }\r\n                        },\r\n                        \"Send_CreatePersonProductRelationMsg_message\": {\r\n                          \"runAfter\": {\r\n                            \"Parse_Active_Product_Relation_Item\": [\r\n                              \"Succeeded\"\r\n                            ]\r\n                          },\r\n                          \"type\": \"ApiConnection\",\r\n                          \"inputs\": {\r\n                            \"body\": {\r\n                              \"ContentData\": \"@{base64(concat('{\\n \\\"correlationId\\\": \\\"',body('Parse_Active_Product_Relation_Item')?['commandId'],'\\\",\\n \\\"personId\\\" : \\\"',triggerBody()?['parameters']?['startMessage']?['entityA'],'\\\",\\n \\\"relationTypeId\\\": \\\"',parameters('GuardianProductRoleId'),'\\\",\\n \\\"productid\\\": \\\"',body('Parse_Active_Product_Relation_Item')?['productId'],'\\\",\\n \\\"productTypeId\\\": \\\"',body('Parse_Active_Product_Relation_Item')?['productTypeId'],'\\\",\\n \\\"startDate\\\":\\\"',triggerBody()?['parameters']?['startMessage']?['fromDate'],'\\\",\\n \\\"endDate\\\": \\\"',triggerBody()?['parameters']?['startMessage']?['toDate'],'\\\"\\n}'))}\",\r\n                              \"ContentType\": \"application/json;charset=utf-8\",\r\n                              \"Properties\": {\r\n                                \"rbs2-content-type\": \"application/json;charset=utf-8\",\r\n                                \"rbs2-corr-id\": \"@{guid()}\",\r\n                                \"rbs2-intent\": \"pub\",\r\n                                \"rbs2-msg-id\": \"@{guid()}\",\r\n                                \"rbs2-msg-type\": \"FiveDegrees.Messages.ProductRelation.CreatePersonProductRelationMsg, FiveDegrees.Messages\",\r\n                                \"rbs2-senttime\": \"@{utcNow()}\",\r\n                                \"x-externalId-id\": \"@{triggerBody()?['parameters']?['externalId']}\",\r\n                                \"x-request-id\": \"@{triggerOutputs()['headers']?['X-Request-ID']}\",\r\n                                \"x-user-id\": \"@{triggerBody()?['parameters']?['createdBy']}\"\r\n                              },\r\n                              \"SessionId\": \"@triggerBody()?['parameters']?['startMessage']?['correlationId']\"\r\n                            },\r\n                            \"host\": {\r\n                              \"connection\": {\r\n                                \"name\": \"@parameters('$connections')['envservicebus']['connectionId']\"\r\n                              }\r\n                            },\r\n                            \"method\": \"post\",\r\n                            \"path\": \"/@{encodeURIComponent(encodeURIComponent('fivedegrees.messages/fivedegrees.messages.productrelation.createpersonproductrelationmsg'))}/messages\"\r\n                          }\r\n                        }\r\n                      },\r\n                      \"runAfter\": {\r\n                        \"Iterate_Over_Wards_Product_Relations\": [\r\n                          \"Succeeded\"\r\n                        ]\r\n                      },\r\n                      \"type\": \"Foreach\"\r\n                    },\r\n                    \"Iterate_Over_All_PersonProduct_Events\": {\r\n                      \"foreach\": \"@body('Parse_Webhook_Response_For_All_PersonProduct_Events')\",\r\n                      \"actions\": {\r\n                        \"Check_If_Person_Product_Relation_Was_Successfully_Created\": {\r\n                          \"actions\": {},\r\n                          \"runAfter\": {\r\n                            \"Parse_Webhook_Response_For_PersonProduct_Event\": [\r\n                              \"Succeeded\"\r\n                            ]\r\n                          },\r\n                          \"else\": {\r\n                            \"actions\": {\r\n                              \"Throw_Exception_-_Because_Person_Product_Relation_Was_Not_Created\": {\r\n                                \"runAfter\": {},\r\n                                \"type\": \"Compose\",\r\n                                \"inputs\": \"@int('ERROR')\"\r\n                              }\r\n                            }\r\n                          },\r\n                          \"expression\": {\r\n                            \"and\": [\r\n                              {\r\n                                \"contains\": [\r\n                                  \"@body('Parse_Webhook_Response_For_PersonProduct_Event')?['Type']\",\r\n                                  \"PersonProductRelationCreated\"\r\n                                ]\r\n                              }\r\n                            ]\r\n                          },\r\n                          \"type\": \"If\"\r\n                        },\r\n                        \"Parse_Webhook_Response_For_PersonProduct_Event\": {\r\n                          \"runAfter\": {},\r\n                          \"type\": \"ParseJson\",\r\n                          \"inputs\": {\r\n                            \"content\": \"@items('Iterate_Over_All_PersonProduct_Events')\",\r\n                            \"schema\": {\r\n                              \"properties\": {\r\n                                \"CorrelationId\": {\r\n                                  \"type\": \"string\"\r\n                                },\r\n                                \"Data\": {\r\n                                  \"properties\": {\r\n                                    \"CorrelationId\": {\r\n                                      \"type\": \"string\"\r\n                                    },\r\n                                    \"RelationId\": {\r\n                                      \"type\": \"string\"\r\n                                    }\r\n                                  },\r\n                                  \"type\": \"object\"\r\n                                },\r\n                                \"Subject\": {\r\n                                  \"type\": \"string\"\r\n                                },\r\n                                \"TimeStamp\": {\r\n                                  \"type\": \"string\"\r\n                                },\r\n                                \"Type\": {\r\n                                  \"type\": \"string\"\r\n                                }\r\n                              },\r\n                              \"type\": \"object\"\r\n                            }\r\n                          }\r\n                        }\r\n                      },\r\n                      \"runAfter\": {\r\n                        \"Parse_Webhook_Response_For_All_PersonProduct_Events\": [\r\n                          \"Succeeded\"\r\n                        ]\r\n                      },\r\n                      \"type\": \"Foreach\"\r\n                    },\r\n                    \"Iterate_Over_Wards_Product_Relations\": {\r\n                      \"foreach\": \"@body('Parse_Product_Relations_Response')?['data']?['productRelations']?['items']\",\r\n                      \"actions\": {\r\n                        \"Check_If_Loan_Exists\": {\r\n                          \"actions\": {},\r\n                          \"runAfter\": {\r\n                            \"Get_Loan_Details_For_Product\": [\r\n                              \"Succeeded\",\r\n                              \"Failed\"\r\n                            ]\r\n                          },\r\n                          \"else\": {\r\n                            \"actions\": {\r\n                              \"Check_If_Loan_Is_NOT_In_Final_Status\": {\r\n                                \"actions\": {\r\n                                  \"Append_Current_ProductId_As_CommandId_To_CommandIds_Variable\": {\r\n                                    \"runAfter\": {\r\n                                      \"Append_Current_ProductRelation_Item_To_ActiveProductRelations_Array_Variable\": [\r\n                                        \"Succeeded\"\r\n                                      ]\r\n                                    },\r\n                                    \"type\": \"AppendToArrayVariable\",\r\n                                    \"inputs\": {\r\n                                      \"name\": \"CommandIds\",\r\n                                      \"value\": \"@variables('CommandId')\"\r\n                                    }\r\n                                  },\r\n                                  \"Append_Current_ProductRelation_Item_To_ActiveProductRelations_Array_Variable\": {\r\n                                    \"runAfter\": {\r\n                                      \"Set_CommandId_Variable\": [\r\n                                        \"Succeeded\"\r\n                                      ]\r\n                                    },\r\n                                    \"type\": \"AppendToArrayVariable\",\r\n                                    \"inputs\": {\r\n                                      \"name\": \"ActiveProductRelations\",\r\n                                      \"value\": {\r\n                                        \"commandId\": \"@{variables('CommandId')}\",\r\n                                        \"productId\": \"@{body('Parse_Product_Relation_Item')?['productId']}\",\r\n                                        \"productTypeId\": \"@{body('Parse_Product_Relation_Item')?['productTypeId']}\",\r\n                                        \"relationTypeId\": \"@{body('Parse_Product_Relation_Item')?['relationTypeId']}\"\r\n                                      }\r\n                                    }\r\n                                  },\r\n                                  \"Set_CommandId_Variable\": {\r\n                                    \"runAfter\": {},\r\n                                    \"type\": \"SetVariable\",\r\n                                    \"inputs\": {\r\n                                      \"name\": \"CommandId\",\r\n                                      \"value\": \"@{guid()}\"\r\n                                    }\r\n                                  }\r\n                                },\r\n                                \"runAfter\": {\r\n                                  \"Parse_Loan_Details_Response\": [\r\n                                    \"Succeeded\"\r\n                                  ]\r\n                                },\r\n                                \"expression\": {\r\n                                  \"and\": [\r\n                                    {\r\n                                      \"not\": {\r\n                                        \"contains\": [\r\n                                          \"@parameters('final_statuses')\",\r\n                                          \"@toLower(body('Parse_loan_details_response')?['loanStatusType'])\"\r\n                                        ]\r\n                                      }\r\n                                    }\r\n                                  ]\r\n                                },\r\n                                \"type\": \"If\"\r\n                              },\r\n                              \"Parse_Loan_Details_Response\": {\r\n                                \"runAfter\": {},\r\n                                \"type\": \"ParseJson\",\r\n                                \"inputs\": {\r\n                                  \"content\": \"@body('Get_Loan_Details_For_Product')\",\r\n                                  \"schema\": {\r\n                                    \"properties\": {\r\n                                      \"inDefaultType\": {\r\n                                        \"type\": \"string\"\r\n                                      },\r\n                                      \"loanStatusType\": {\r\n                                        \"type\": \"string\"\r\n                                      }\r\n                                    },\r\n                                    \"type\": \"object\"\r\n                                  }\r\n                                }\r\n                              }\r\n                            }\r\n                          },\r\n                          \"expression\": {\r\n                            \"and\": [\r\n                              {\r\n                                \"equals\": [\r\n                                  \"@outputs('Get_Loan_Details_For_Product')['statusCode']\",\r\n                                  404\r\n                                ]\r\n                              }\r\n                            ]\r\n                          },\r\n                          \"type\": \"If\"\r\n                        },\r\n                        \"Get_Loan_Details_For_Product\": {\r\n                          \"runAfter\": {\r\n                            \"Parse_Product_Relation_Item\": [\r\n                              \"Succeeded\"\r\n                            ]\r\n                          },\r\n                          \"type\": \"Http\",\r\n                          \"inputs\": {\r\n                            \"authentication\": {\r\n                              \"type\": \"ManagedServiceIdentity\"\r\n                            },\r\n                            \"headers\": {\r\n                              \"Ocp-Apim-Subscription-Key\": \"a73bf2a1-87a8-a9bb-57c9-4e22a422601a\",\r\n                              \"Ocp-Apim-Trace\": \"true\",\r\n                              \"x-request-id\": \"@{triggerOutputs()?['headers']?['X-Request-ID']}\",\r\n                              \"x-user-id\": \"@triggerBody()?['parameters']?['createdBy']\"\r\n                            },\r\n                            \"method\": \"GET\",\r\n                            \"uri\": \"@{parameters('HTTP_Get_Loan_Data-URI')}/@{body('Parse_Product_Relation_Item')?['productId']}/status\"\r\n                          }\r\n                        },\r\n                        \"Parse_Product_Relation_Item\": {\r\n                          \"runAfter\": {},\r\n                          \"type\": \"ParseJson\",\r\n                          \"inputs\": {\r\n                            \"content\": \"@items('Iterate_Over_Wards_Product_Relations')\",\r\n                            \"schema\": {\r\n                              \"properties\": {\r\n                                \"productId\": {\r\n                                  \"type\": \"string\"\r\n                                },\r\n                                \"productTypeId\": {\r\n                                  \"type\": \"string\"\r\n                                },\r\n                                \"relationTypeId\": {\r\n                                  \"type\": \"string\"\r\n                                }\r\n                              },\r\n                              \"type\": \"object\"\r\n                            }\r\n                          }\r\n                        }\r\n                      },\r\n                      \"runAfter\": {},\r\n                      \"type\": \"Foreach\",\r\n                      \"runtimeConfiguration\": {\r\n                        \"concurrency\": {\r\n                          \"repetitions\": 1\r\n                        }\r\n                      }\r\n                    },\r\n                    \"Parse_Webhook_Response_For_All_PersonProduct_Events\": {\r\n                      \"runAfter\": {\r\n                        \"HTTP_Webhook_-_Subscribe_To_Event_Gateway_For_All_PersonProduct_Events\": [\r\n                          \"Succeeded\"\r\n                        ]\r\n                      },\r\n                      \"type\": \"ParseJson\",\r\n                      \"inputs\": {\r\n                        \"content\": \"@body('HTTP_Webhook_-_Subscribe_To_Event_Gateway_For_All_PersonProduct_Events')\",\r\n                        \"schema\": {\r\n                          \"items\": {\r\n                            \"properties\": {\r\n                              \"CorrelationId\": {\r\n                                \"type\": \"string\"\r\n                              },\r\n                              \"Data\": {\r\n                                \"properties\": {\r\n                                  \"CorrelationId\": {\r\n                                    \"type\": \"string\"\r\n                                  },\r\n                                  \"RelationId\": {\r\n                                    \"type\": \"string\"\r\n                                  }\r\n                                },\r\n                                \"type\": \"object\"\r\n                              },\r\n                              \"Subject\": {\r\n                                \"type\": \"string\"\r\n                              },\r\n                              \"TimeStamp\": {\r\n                                \"type\": \"string\"\r\n                              },\r\n                              \"Type\": {\r\n                                \"type\": \"string\"\r\n                              }\r\n                            },\r\n                            \"type\": \"object\"\r\n                          },\r\n                          \"type\": \"array\"\r\n                        }\r\n                      }\r\n                    }\r\n                  },\r\n                  \"runAfter\": {\r\n                    \"Parse_Product_Relations_Response\": [\r\n                      \"Succeeded\"\r\n                    ]\r\n                  },\r\n                  \"expression\": {\r\n                    \"and\": [\r\n                      {\r\n                        \"equals\": [\r\n                          \"@empty(body('Parse_Product_Relations_Response')?['data']?['productRelations']?['items'])\",\r\n                          false\r\n                        ]\r\n                      }\r\n                    ]\r\n                  },\r\n                  \"type\": \"If\"\r\n                },\r\n                \"Get_Product_Relations_For_Ward_\": {\r\n                  \"runAfter\": {},\r\n                  \"type\": \"Http\",\r\n                  \"inputs\": {\r\n                    \"authentication\": {\r\n                      \"type\": \"ManagedServiceIdentity\"\r\n                    },\r\n                    \"headers\": {\r\n                      \"Ocp-Apim-Subscription-Key\": \"a73bf2a1-87a8-a9bb-57c9-4e22a422601a\",\r\n                      \"x-external-id\": \"@triggerBody()?['parameters']?['externalId']\",\r\n                      \"x-request-id\": \"@{triggerOutputs()['headers']?['X-Request-ID']}\",\r\n                      \"x-user-id\": \"@triggerBody()?['parameters']?['createdBy']\"\r\n                    },\r\n                    \"method\": \"GET\",\r\n                    \"queries\": {\r\n                      \"query\": \"{\\n  productRelations(\\n    where: [{ path: \\\"personId\\\", comparison: \\\"equal\\\", value: \\\"@{triggerBody()?['parameters']?['startMessage']?['entityB']}\\\" }, ]\\n  ) {\\n    items {\\n      productId\\n      relationTypeId\\n      productTypeId\\n    }\\n  }\\n}\"\r\n                    },\r\n                    \"uri\": \"https://apim.orc.my.fivedegrees.cloud/productrelation-sbx/api/graphql\"\r\n                  }\r\n                },\r\n                \"Parse_Product_Relations_Response\": {\r\n                  \"runAfter\": {\r\n                    \"Get_Product_Relations_For_Ward_\": [\r\n                      \"Succeeded\"\r\n                    ]\r\n                  },\r\n                  \"type\": \"ParseJson\",\r\n                  \"inputs\": {\r\n                    \"content\": \"@body('Get_Product_Relations_For_Ward_')\",\r\n                    \"schema\": {\r\n                      \"properties\": {\r\n                        \"data\": {\r\n                          \"properties\": {\r\n                            \"productRelations\": {\r\n                              \"properties\": {\r\n                                \"items\": {\r\n                                  \"items\": {\r\n                                    \"properties\": {\r\n                                      \"productId\": {\r\n                                        \"type\": \"string\"\r\n                                      },\r\n                                      \"productTypeId\": {\r\n                                        \"type\": \"string\"\r\n                                      },\r\n                                      \"relationTypeId\": {\r\n                                        \"type\": \"string\"\r\n                                      }\r\n                                    },\r\n                                    \"required\": [\r\n                                      \"productId\",\r\n                                      \"relationTypeId\",\r\n                                      \"productTypeId\"\r\n                                    ],\r\n                                    \"type\": \"object\"\r\n                                  },\r\n                                  \"type\": \"array\"\r\n                                }\r\n                              },\r\n                              \"type\": \"object\"\r\n                            }\r\n                          },\r\n                          \"type\": \"object\"\r\n                        }\r\n                      },\r\n                      \"type\": \"object\"\r\n                    }\r\n                  }\r\n                }\r\n              },\r\n              \"runAfter\": {\r\n                \"Check_If_Guardian_Relation_Is_Created\": [\r\n                  \"Succeeded\"\r\n                ]\r\n              },\r\n              \"expression\": {\r\n                \"and\": [\r\n                  {\r\n                    \"equals\": [\r\n                      \"@variables('IsGuardian')\",\r\n                      \"@true\"\r\n                    ]\r\n                  }\r\n                ]\r\n              },\r\n              \"type\": \"If\"\r\n            },\r\n            \"Parse_Webhook_Response\": {\r\n              \"runAfter\": {\r\n                \"HTTP_Webhook_Subscribe_To_Event_Gateway_For_EntityRelation_Event\": [\r\n                  \"Succeeded\"\r\n                ]\r\n              },\r\n              \"type\": \"ParseJson\",\r\n              \"inputs\": {\r\n                \"content\": \"@body('HTTP_Webhook_Subscribe_To_Event_Gateway_For_EntityRelation_Event')\",\r\n                \"schema\": {\r\n                  \"items\": {\r\n                    \"properties\": {\r\n                      \"CorrelationId\": {\r\n                        \"type\": \"string\"\r\n                      },\r\n                      \"Data\": {\r\n                        \"properties\": {\r\n                          \"CorrelationId\": {\r\n                            \"type\": \"string\"\r\n                          },\r\n                          \"RelationId\": {\r\n                            \"type\": \"string\"\r\n                          }\r\n                        },\r\n                        \"type\": \"object\"\r\n                      },\r\n                      \"Subject\": {\r\n                        \"type\": \"string\"\r\n                      },\r\n                      \"TimeStamp\": {\r\n                        \"type\": \"string\"\r\n                      },\r\n                      \"Type\": {\r\n                        \"type\": \"string\"\r\n                      }\r\n                    },\r\n                    \"type\": \"object\"\r\n                  },\r\n                  \"type\": \"array\"\r\n                }\r\n              }\r\n            },\r\n            \"Send_CreateEntityRelationMsg_Message\": {\r\n              \"runAfter\": {\r\n                \"Check_If_Manual_Task_Is_Required\": [\r\n                  \"Succeeded\"\r\n                ]\r\n              },\r\n              \"type\": \"ApiConnection\",\r\n              \"inputs\": {\r\n                \"body\": {\r\n                  \"ContentData\": \"@{base64(concat('{','\\n','\\\"correlationId\\\":\\\"',triggerBody()?['parameters']?['startMessage']?['correlationId'],'\\\",','\\n','\\\"entityA\\\":\\\"',triggerBody()?['parameters']?['startMessage']?['entityA'],'\\\",','\\n','\\\"entityB\\\":\\\"',triggerBody()?['parameters']?['startMessage']?['entityB'],'\\\",','\\n','\\\"relationType\\\":\\\"',parameters('GuardianRelationType'),'\\\",','\\n','\\\"fromDate\\\":\\\"',triggerBody()?['parameters']?['startMessage']?['fromDate'],'\\\",','\\n','\\\"toDate\\\":\\\"',triggerBody()?['parameters']?['startMessage']?['toDate'],'\\\"','\\n','}'))}\",\r\n                  \"ContentType\": \"application/json;charset=utf-8\",\r\n                  \"Properties\": {\r\n                    \"rbs2-content-type\": \"application/json;charset=utf-8\",\r\n                    \"rbs2-corr-id\": \"@{guid()}\",\r\n                    \"rbs2-intent\": \"pub\",\r\n                    \"rbs2-msg-id\": \"@{guid()}\",\r\n                    \"rbs2-msg-type\": \"FiveDegrees.Messages.EntityRelation.CreateEntityRelationMsg, FiveDegrees.Messages\",\r\n                    \"rbs2-senttime\": \"@{utcNow()}\",\r\n                    \"x-externalId-id\": \"@{triggerBody()?['parameters']?['externalId']}\",\r\n                    \"x-request-id\": \"@{triggerOutputs()['headers']?['X-Request-ID']}\",\r\n                    \"x-user-id\": \"@{triggerBody()?['parameters']?['createdBy']}\"\r\n                  }\r\n                },\r\n                \"host\": {\r\n                  \"connection\": {\r\n                    \"name\": \"@parameters('$connections')['envservicebus']['connectionId']\"\r\n                  }\r\n                },\r\n                \"method\": \"post\",\r\n                \"path\": \"/@{encodeURIComponent(encodeURIComponent('fivedegrees.messages/fivedegrees.messages.entityrelation.createentityrelationmsg'))}/messages\"\r\n              }\r\n            }\r\n          },\r\n          \"runAfter\": {\r\n            \"Initialize_ActiveProductRelations_variable\": [\r\n              \"Succeeded\"\r\n            ]\r\n          },\r\n          \"type\": \"Scope\"\r\n        },\r\n        \"Scope_-_Finally\": {\r\n          \"actions\": {\r\n            \"Publish_CreateGuardianRelation_Event\": {\r\n              \"runAfter\": {\r\n                \"Send_UpdateProcessStatusMsg_Message\": [\r\n                  \"Succeeded\"\r\n                ]\r\n              },\r\n              \"type\": \"ApiConnection\",\r\n              \"inputs\": {\r\n                \"body\": [\r\n                  {\r\n                    \"data\": \"@triggerBody()?['parameters']?['startMessage']\",\r\n                    \"eventType\": \"CreateGuardianRelation@{actions('Scope_-_Create_Guardian')}\",\r\n                    \"id\": \"@triggerBody()?['parameters']?['startMessage']?['correlationId']\",\r\n                    \"subject\": \"api/entityrelations/@{body('Parse_Webhook_Response')?[0]?['RelationId']}\",\r\n                    \"topic\": \"entityrelations\"\r\n                  }\r\n                ],\r\n                \"host\": {\r\n                  \"connection\": {\r\n                    \"name\": \"@parameters('$connections')['enveventgrid']['connectionId']\"\r\n                  }\r\n                },\r\n                \"method\": \"post\",\r\n                \"path\": \"/eventGrid/api/events\"\r\n              }\r\n            },\r\n            \"Send_UpdateProcessStatusMsg_Message\": {\r\n              \"runAfter\": {},\r\n              \"type\": \"ApiConnection\",\r\n              \"inputs\": {\r\n                \"body\": {\r\n                  \"ContentData\": \"@{base64(concat('{','\\n','  \\\"correlationId\\\":\\\"',triggerBody()?['parameters']?['startMessage']?['correlationId'],'\\\",','\\n','  \\\"operationId\\\":\\\"',triggerBody()?['parameters']?['operationId'],'\\\",','\\n','  \\\"status\\\":\\\"',toLower(actions('Scope_-_Create_Guardian')?['status']),'\\\",','\\n','  \\\"endDate\\\":\\\"',actions('Scope_-_Create_Guardian')?['endTime'],'\\\"','\\n','}'))}\",\r\n                  \"ContentType\": \"application/json;charset=utf-8\",\r\n                  \"Properties\": {\r\n                    \"rbs2-content-type\": \"application/json;charset=utf-8\",\r\n                    \"rbs2-corr-id\": \"@{guid()}\",\r\n                    \"rbs2-intent\": \"pub\",\r\n                    \"rbs2-msg-id\": \"@{guid()}\",\r\n                    \"rbs2-msg-type\": \"FiveDegrees.Messages.ProcessManager.UpdateProcessStatusMsg, FiveDegrees.Messages\",\r\n                    \"rbs2-senttime\": \"@{utcNow()}\",\r\n                    \"x-externalId-id\": \"@{triggerBody()?['parameters']?['externalId']}\",\r\n                    \"x-request-id\": \"@{triggerOutputs()['headers']?['X-Request-ID']}\",\r\n                    \"x-user-id\": \"@{triggerBody()?['parameters']?['createdBy']}\"\r\n                  },\r\n                  \"SessionId\": \"@triggerBody()?['parameters']?['startMessage']?['correlationId']\"\r\n                },\r\n                \"host\": {\r\n                  \"connection\": {\r\n                    \"name\": \"@parameters('$connections')['envservicebus']['connectionId']\"\r\n                  }\r\n                },\r\n                \"method\": \"post\",\r\n                \"path\": \"/@{encodeURIComponent(encodeURIComponent('fivedegrees.messages/fivedegrees.messages.processmanager.updateprocessstatusmsg'))}/messages\",\r\n                \"queries\": {\r\n                  \"systemProperties\": \"None\"\r\n                }\r\n              }\r\n            }\r\n          },\r\n          \"runAfter\": {\r\n            \"Scope_-_Create_Guardian\": [\r\n              \"Failed\",\r\n              \"Skipped\",\r\n              \"Succeeded\",\r\n              \"TimedOut\"\r\n            ]\r\n          },\r\n          \"type\": \"Scope\"\r\n        }\r\n      },\r\n      \"outputs\": {}\r\n    },\r\n    \"parameters\": {\r\n      \"$connections\": {\r\n        \"value\": {\r\n          \"enveventgrid\": {\r\n            \"id\": \"/subscriptions/5023f8ef-7ec0-4877-8103-b3b9bac698d1/providers/Microsoft.Web/locations/westeurope/managedApis/azureeventgridpublish\",\r\n            \"connectionId\": \"/subscriptions/755f2cc1-0d1c-40da-b6ad-9bbfe83a6d7a/resourceGroups/devpfm-ork-sbx-rg/providers/Microsoft.Web/connections/orcsbxsharedeventgriddomain\",\r\n            \"connectionName\": \"orcsbxsharedeventgriddomain\"\r\n          },\r\n          \"envservicebus\": {\r\n            \"id\": \"/subscriptions/5023f8ef-7ec0-4877-8103-b3b9bac698d1/providers/Microsoft.Web/locations/westeurope/managedApis/servicebus\",\r\n            \"connectionId\": \"/subscriptions/755f2cc1-0d1c-40da-b6ad-9bbfe83a6d7a/resourceGroups/devpfm-ork-sbx-rg/providers/Microsoft.Web/connections/orcsbxservicebus\",\r\n            \"connectionName\": \"orcsbxservicebus\"\r\n          }\r\n        }\r\n      }\r\n    },\r\n    \"endpointsConfiguration\": {\r\n      \"workflow\": {\r\n        \"outgoingIpAddresses\": [\r\n          {\r\n            \"address\": \"40.68.222.65\"\r\n          },\r\n          {\r\n            \"address\": \"40.68.209.23\"\r\n          },\r\n          {\r\n            \"address\": \"13.95.147.65\"\r\n          },\r\n          {\r\n            \"address\": \"23.97.218.130\"\r\n          },\r\n          {\r\n            \"address\": \"51.144.182.201\"\r\n          },\r\n          {\r\n            \"address\": \"23.97.211.179\"\r\n          },\r\n          {\r\n            \"address\": \"104.45.9.52\"\r\n          },\r\n          {\r\n            \"address\": \"23.97.210.126\"\r\n          },\r\n          {\r\n            \"address\": \"13.69.71.160\"\r\n          },\r\n          {\r\n            \"address\": \"13.69.71.161\"\r\n          },\r\n          {\r\n            \"address\": \"13.69.71.162\"\r\n          },\r\n          {\r\n            \"address\": \"13.69.71.163\"\r\n          },\r\n          {\r\n            \"address\": \"13.69.71.164\"\r\n          },\r\n          {\r\n            \"address\": \"13.69.71.165\"\r\n          },\r\n          {\r\n            \"address\": \"13.69.71.166\"\r\n          },\r\n          {\r\n            \"address\": \"13.69.71.167\"\r\n          }\r\n        ],\r\n        \"accessEndpointIpAddresses\": [\r\n          {\r\n            \"address\": \"13.95.155.53\"\r\n          },\r\n          {\r\n            \"address\": \"52.174.54.218\"\r\n          },\r\n          {\r\n            \"address\": \"52.174.49.6\"\r\n          },\r\n          {\r\n            \"address\": \"52.174.49.6\"\r\n          }\r\n        ]\r\n      },\r\n      \"connector\": {\r\n        \"outgoingIpAddresses\": [\r\n          {\r\n            \"address\": \"13.69.64.208/28\"\r\n          },\r\n          {\r\n            \"address\": \"40.115.50.13\"\r\n          },\r\n          {\r\n            \"address\": \"52.174.88.118\"\r\n          },\r\n          {\r\n            \"address\": \"40.91.208.65\"\r\n          },\r\n          {\r\n            \"address\": \"52.166.78.89\"\r\n          }\r\n        ]\r\n      }\r\n    }\r\n  },\r\n  \"id\": \"/subscriptions/755f2cc1-0d1c-40da-b6ad-9bbfe83a6d7a/resourceGroups/devpfm-ork-sbx-rg/providers/Microsoft.Logic/workflows/Create-Guardian\",\r\n  \"name\": \"Create-Guardian\",\r\n  \"type\": \"Microsoft.Logic/workflows\",\r\n  \"location\": \"westeurope\",\r\n  \"identity\": {\r\n    \"type\": \"SystemAssigned\",\r\n    \"principalId\": \"28c7aa06-1169-4091-9a07-54153cd64ba9\",\r\n    \"tenantId\": \"e4b3b9a7-2979-4802-9f83-bb188be7c422\"\r\n  }\r\n}";
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.OK, azureResponse);
            var mockConfig = new Mock<IConfigurationService>();
            mockCache.Setup(cache => cache.GetOrSetValueAsync(It.IsAny<Func<Task<Guid>>>(), It.IsAny<string>(), null, null, default))
                .ReturnsAsync(Guid.Parse("28c7aa06-1169-4091-9a07-54153cd64ba9"));
            var subjectUnderTest = new LogicAppProcessService("", "", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);

            // ACT
            var principalId = await subjectUnderTest
                .GetPrincipalIdAsync("LA-Test", null);

            // ASSERT
            mockCache.Verify();

            Assert.Equal(Guid.Parse("28c7aa06-1169-4091-9a07-54153cd64ba9"), principalId);
        }

        [Fact]
        public async Task GetProcessWithMessage_Invalid_Key_Throws()
        {
            string azureResponse = "{ \"properties\": { } }";
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.OK, azureResponse);
            var mockConfig = new Mock<IConfigurationService>();
            mockCache.Setup(cache => cache.GetOrSetValueAsync<string>(It.IsAny<Func<Task<string>>>(), It.IsAny<string>(), null, null, default)).Throws(new InvalidOperationException());

            // ARRANGE
            var subjectUnderTest = new LogicAppProcessService("", "", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);

            // ACT
            // ASSERT
            await Assert.ThrowsAsync<InvalidOperationException>(() => subjectUnderTest
               .GetProcessWithMessageAsync("123", "key", new { Test = "test" }, null));
        }

        [Fact]
        public async Task GetPrincipalId_Invalid_Key_Throws()
        {
            string azureResponse = "{ \"properties\": { } }";
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.OK, azureResponse);
            var mockConfig = new Mock<IConfigurationService>();
            mockCache.Setup(cr => cr.GetOrSetValueAsync(It.IsAny<Func<Task<Guid>>>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());

            // ARRANGE
            var subjectUnderTest = new LogicAppProcessService("", "", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);

            // ACT
            // ASSERT
            await Assert.ThrowsAsync<InvalidOperationException>(() => subjectUnderTest
                .GetPrincipalIdAsync("123", null));
        }

        [Fact]
        public async Task StartProcess_Process_Null_Throws()
        {
            string azureResponse = "{ \"properties\": { } }";
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.OK, azureResponse);
            var mockConfig = new Mock<IConfigurationService>();

            // ARRANGE
            var subjectUnderTest = new LogicAppProcessService("", "", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);

            // ACT
            // ASSERT
            await Assert.ThrowsAsync<ArgumentNullException>(() => subjectUnderTest
               .StartProcessAsync(null, null));
        }

        [Fact]
        public async Task StartProcess_Azure_Unavailable_Throws()
        {
            string azureResponse = "{ \"properties\": { } }";
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.BadRequest, azureResponse);
            var mockConfig = new Mock<IConfigurationService>();
            mockConfig.Setup(c => c.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync("[]");

            var process = new Process() { StartUrl = "http://test.com/", Parameters = new JObject() };
            var subjectUnderTest = new LogicAppProcessService("", "", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);

            // ACT
            // ASSERT
            await Assert.ThrowsAsync<HttpRequestException>(() => subjectUnderTest
               .StartProcessAsync(process, new Dictionary<string, string> { { "x-user-id", Guid.NewGuid().ToString() } }));
        }

        [Fact]
        public async Task StartProcess_Valid_Process_Post_Trigger_Accepted()
        {
            // ARRANGE
            string azureResponse = "{ \"properties\": { } }";
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.OK, azureResponse);
            var mockConfig = new Mock<IConfigurationService>();
            mockConfig.Setup(c => c.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync("[]");

            var process = new Process { StartUrl = @"http://test.com/", Parameters = new JObject() };
            var subjectUnderTest = new LogicAppProcessService("", "", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);

            // ACT
            await subjectUnderTest
               .StartProcessAsync(process, new Dictionary<string, string> { { "x-user-id", Guid.NewGuid().ToString() } });

            // ASSERT
            mockHandler.Verify();
        }

        private (Mock<IHttpClientFactory>, Mock<HttpMessageHandler>, Mock<IAzureTokenProvider>) GetMockClientFactoryWithResponse(HttpStatusCode statusCode, string content)
        {
            var mockTokenProvider = new Mock<IAzureTokenProvider>();
            mockTokenProvider.Setup(s => s.GetAuthorizationTokenAsync(It.IsAny<string>())).ReturnsAsync(new System.Net.Http.Headers.AuthenticationHeaderValue("asd"));

            var message = new HttpResponseMessage()
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            };

            message.Headers.Add("x-ms-workflow-run-id", Guid.NewGuid().ToString());

            var mockHttpHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            mockHttpHandler
               .Protected()
               // Setup the PROTECTED method to mock
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               // prepare the expected response of the mocked http call
               .ReturnsAsync(message)
               .Verifiable();

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(s => s.CreateClient(It.IsAny<string>())).Returns(new HttpClient(mockHttpHandler.Object));

            return (mockHttpClientFactory, mockHttpHandler, mockTokenProvider);
        }

        [Fact]
        public async Task StartProcess_Valid_Process_With_Valid_Configuration_Post_Trigger_Accepted()
        {
            // ARRANGE
            string azureResponse = "{ \"properties\": { } }";
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.OK, azureResponse);
            var mockConfig = new Mock<IConfigurationService>();
            mockConfig.Setup(c => c.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync("[{'SettingKey':'BoolSettingKey','BoolValue':true,'ValueType':'Bool'},{'SettingKey':'IntSettingKey','IntValue':0,'ValueType':'Int'},{'SettingKey':'StringSettingKey','StringValue':'true','ValueType':'String'},{'SettingKey':'GuidSettingKey','GuidValue':'00000000-0000-0000-0000-000000000000','ValueType':'Guid'}]");

            var process = new Process { StartUrl = @"http://test.com/", Parameters = new JObject() };
            var subjectUnderTest = new LogicAppProcessService("", "", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);

            // ACT
            await subjectUnderTest
               .StartProcessAsync(process, new Dictionary<string, string> { { "x-user-id", Guid.NewGuid().ToString() } });

            // ASSERT
            mockHandler.Verify();
        }

        [Fact]
        public async Task StartProcess_Valid_Process_With_Invalid_Configuration_Post_Trigger_Accepted()
        {
            // ARRANGE
            string azureResponse = "{ \"properties\": { } }";
            var (mockFactory, mockHandler, mockTokenProvider) = GetMockClientFactoryWithResponse(HttpStatusCode.OK, azureResponse);
            var mockConfig = new Mock<IConfigurationService>();
            mockConfig.Setup(c => c.GetConfigurationAsync(It.IsAny<string>()))
                .ReturnsAsync("[{'SettingKey':'NullSettingKey'}]");

            var process = new Process { StartUrl = @"http://test.com/", Parameters = new JObject() };
            var subjectUnderTest = new LogicAppProcessService("", "", mockFactory.Object, mockTokenProvider.Object, mockConfig.Object, mockCache.Object);

            // ACT

            // ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() => subjectUnderTest
               .StartProcessAsync(process, new Dictionary<string, string> { { "x-user-id", Guid.NewGuid().ToString() } }));
        }
    }
}
