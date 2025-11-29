using Autofac;
using Autofac.Extensions.DependencyInjection;
using GraphQL.EntityFramework.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using ProcessManager.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace ProcessManager.Tests.IntegrationTests.API
{
    public class GraphQLControllerTests : TestFixture
    {
        private readonly IHost _host;
        private readonly HttpClient _client;

        public GraphQLControllerTests() : base()
        {
            _host = hostBuilder.Start();
            _client = _host.GetTestClient();
            ClientQueryExecutor.SetQueryUri("/api/graphql");
        }

        [Fact]
        public async Task Query_Processes_Returns_All_Processes()
        {
            // Arrange
            var processes = GetWorkflowRunDbos();
            await SaveProcessesToDb(processes);

            // Act
            var query = @"{  
                processes
                {
                    items{
                       operationId
                       workflowRunName
                       status
                       createdBy
                       createdDate
                       changedBy
                       changedDate
                       relations{
                          entityId
                          entityType
                       }
                    }
                }
            }";

            using var response = await ClientQueryExecutor.ExecuteGet(_client, query);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(processes[0].OperationId, (Guid)jobj["data"]["processes"]["items"][0]["operationId"]);
            Assert.Equal(processes[0].WorkflowRunName, (string)jobj["data"]["processes"]["items"][0]["workflowRunName"]);
            Assert.Equal(processes[0].Status, (string)jobj["data"]["processes"]["items"][0]["status"]);
            Assert.Equal(processes[0].CreatedBy, (string)jobj["data"]["processes"]["items"][0]["createdBy"]);
            Assert.Equal(processes[0].ChangedBy, (string)jobj["data"]["processes"]["items"][0]["changedBy"]);

            Assert.Equal(processes[1].OperationId, (Guid)jobj["data"]["processes"]["items"][1]["operationId"]);
            Assert.Equal(processes[1].WorkflowRunName, (string)jobj["data"]["processes"]["items"][1]["workflowRunName"]);
            Assert.Equal(processes[1].Status, (string)jobj["data"]["processes"]["items"][1]["status"]);
            Assert.Equal(processes[1].CreatedBy, (string)jobj["data"]["processes"]["items"][1]["createdBy"]);
            Assert.Equal(processes[1].ChangedBy, (string)jobj["data"]["processes"]["items"][1]["changedBy"]);

            Assert.Equal(processes[2].OperationId, (Guid)jobj["data"]["processes"]["items"][2]["operationId"]);
            Assert.Equal(processes[2].WorkflowRunName, (string)jobj["data"]["processes"]["items"][2]["workflowRunName"]);
            Assert.Equal(processes[2].Status, (string)jobj["data"]["processes"]["items"][2]["status"]);
            Assert.Equal(processes[2].CreatedBy, (string)jobj["data"]["processes"]["items"][2]["createdBy"]);
            Assert.Equal(processes[2].ChangedBy, (string)jobj["data"]["processes"]["items"][2]["changedBy"]);
        }

        [Fact]
        public async Task Query_Processes_With_Activities_Returns_All_Processes_With_Activities()
        {
            // Arrange
            var processes = GetWorkflowRunDbos();
            await SaveProcessesToDb(processes);

            // Act
            var query = @"{  
                processes
                {
                    items{
                       operationId
                       workflowRunName
                       status
                       createdBy
                       createdDate
                       changedBy
                       changedDate
                       relations{
                          entityId
                          entityType
                       }
                       activities{
                          activityId
                          name
                          status
                          startDate
                          endDate
                       }
                    }
                }
            }";

            using var response = await ClientQueryExecutor.ExecuteGet(_client, query);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(processes[0].OperationId, (Guid)jobj["data"]["processes"]["items"][0]["operationId"]);
            Assert.Equal(processes[0].WorkflowRunName, (string)jobj["data"]["processes"]["items"][0]["workflowRunName"]);
            Assert.Equal(processes[0].Status, (string)jobj["data"]["processes"]["items"][0]["status"]);
            Assert.Equal(processes[0].CreatedBy, (string)jobj["data"]["processes"]["items"][0]["createdBy"]);
            Assert.Equal(processes[0].ChangedBy, (string)jobj["data"]["processes"]["items"][0]["changedBy"]);

            Assert.Equal(processes[1].OperationId, (Guid)jobj["data"]["processes"]["items"][1]["operationId"]);
            Assert.Equal(processes[1].WorkflowRunName, (string)jobj["data"]["processes"]["items"][1]["workflowRunName"]);
            Assert.Equal(processes[1].Status, (string)jobj["data"]["processes"]["items"][1]["status"]);
            Assert.Equal(processes[1].CreatedBy, (string)jobj["data"]["processes"]["items"][1]["createdBy"]);
            Assert.Equal(processes[1].ChangedBy, (string)jobj["data"]["processes"]["items"][1]["changedBy"]);

            Assert.Equal(processes[2].OperationId, (Guid)jobj["data"]["processes"]["items"][2]["operationId"]);
            Assert.Equal(processes[2].WorkflowRunName, (string)jobj["data"]["processes"]["items"][2]["workflowRunName"]);
            Assert.Equal(processes[2].Status, (string)jobj["data"]["processes"]["items"][2]["status"]);
            Assert.Equal(processes[2].CreatedBy, (string)jobj["data"]["processes"]["items"][2]["createdBy"]);
            Assert.Equal(processes[2].ChangedBy, (string)jobj["data"]["processes"]["items"][2]["changedBy"]);
        }

        [Fact]
        public async Task Query_Activities_Returns_All_Activities()
        {
            // Arrange
            var activities = GetActivityDbos();
            await SaveActivitiesToDb(activities);

            // Act
            var query = @"{  
                activities
                {
                    items{
                       activityId
                       name
                       status
                       startDate
                       endDate
                       operationId
                    }
                }
            }";

            using var response = await ClientQueryExecutor.ExecuteGet(_client, query);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(activities[0].ActivityId, (Guid)jobj["data"]["activities"]["items"][0]["activityId"]);
            Assert.Equal(activities[0].Name, (string)jobj["data"]["activities"]["items"][0]["name"]);
            Assert.Equal(activities[0].Status, (string)jobj["data"]["activities"]["items"][0]["status"]);
            Assert.Equal(activities[0].StartDate, (DateTime)jobj["data"]["activities"]["items"][0]["startDate"]);
            Assert.Equal(activities[0].EndDate, (DateTime)jobj["data"]["activities"]["items"][0]["endDate"]);
            Assert.Equal(activities[0].OperationId, (Guid)jobj["data"]["activities"]["items"][0]["operationId"]);

            Assert.Equal(activities[1].ActivityId, (Guid)jobj["data"]["activities"]["items"][1]["activityId"]);
            Assert.Equal(activities[1].Name, (string)jobj["data"]["activities"]["items"][1]["name"]);
            Assert.Equal(activities[1].Status, (string)jobj["data"]["activities"]["items"][1]["status"]);
            Assert.Equal(activities[1].StartDate, (DateTime)jobj["data"]["activities"]["items"][1]["startDate"]);
            Assert.Equal(activities[1].EndDate, (DateTime)jobj["data"]["activities"]["items"][1]["endDate"]);
            Assert.Equal(activities[1].OperationId, (Guid)jobj["data"]["activities"]["items"][1]["operationId"]);

            Assert.Equal(activities[2].ActivityId, (Guid)jobj["data"]["activities"]["items"][2]["activityId"]);
            Assert.Equal(activities[2].Name, (string)jobj["data"]["activities"]["items"][2]["name"]);
            Assert.Equal(activities[2].Status, (string)jobj["data"]["activities"]["items"][2]["status"]);
            Assert.Equal(activities[2].StartDate, (DateTime)jobj["data"]["activities"]["items"][2]["startDate"]);
            Assert.Equal(activities[2].EndDate, (DateTime)jobj["data"]["activities"]["items"][2]["endDate"]);
            Assert.Equal(activities[2].OperationId, (Guid)jobj["data"]["activities"]["items"][2]["operationId"]);
        }

            [Fact]
        public async Task Query_Relations_Returns_All_Relations()
        {
            // Arrange
            var relations = GetRelationDbos();
            await SaveRelationsToDb(relations);

            // Act
            var query = @"{  
                relations
                {
                    items{
                       entityId
                       entityType
                    }
                }
            }";

            using var response = await ClientQueryExecutor.ExecuteGet(_client, query);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var relation = relations[0];
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(relation.EntityId, (Guid)jobj["data"]["relations"]["items"][0]["entityId"]);
        }

        [Fact]
        public async Task Query_Processes_By_Id_With_Variables_Returns_Process()
        {
            // Arrange
            var processes = GetWorkflowRunDbos();
            await SaveProcessesToDb(processes);

            // Act
            var expectedprocess = processes[0];
            var variables = new
            {
                id = expectedprocess.OperationId
            };
            var query = @"
                query ($id: ID!) {
                    processes(ids:[$id]) {
                         items{
                             operationId
                             workflowRunName
                             status
                             createdBy
                             createdDate
                             changedBy
                             changedDate
                        }
                    }
                }";

            using var response = await ClientQueryExecutor.ExecuteGet(_client, query, variables);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(processes[0].OperationId, (Guid)jobj["data"]["processes"]["items"][0]["operationId"]);
            Assert.Equal(processes[0].WorkflowRunName, (string)jobj["data"]["processes"]["items"][0]["workflowRunName"]);
            Assert.Equal(processes[0].Status, (string)jobj["data"]["processes"]["items"][0]["status"]);
            Assert.Equal(processes[0].CreatedBy, (string)jobj["data"]["processes"]["items"][0]["createdBy"]);
            Assert.Equal(processes[0].ChangedBy, (string)jobj["data"]["processes"]["items"][0]["changedBy"]);
        }

        [Fact]
        public async Task Query_Activities_By_Id_With_Variables_Returns_Activity()
        {
            // Arrange
            var activities = GetActivityDbos();
            await SaveActivitiesToDb(activities);

            // Act
            var expectedActivity = activities[0];
            var variables = new
            {
                id = expectedActivity.ActivityId
            };
            var query = @"
                query ($id: ID!) {
                    activities(ids:[$id]) {
                         items{
                             operationId
                             name
                             status
                             startDate
                             endDate
                        }
                    }
                }";

            using var response = await ClientQueryExecutor.ExecuteGet(_client, query, variables);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(activities[0].OperationId, (Guid)jobj["data"]["activities"]["items"][0]["operationId"]);
            Assert.Equal(activities[0].Name, (string)jobj["data"]["activities"]["items"][0]["name"]);
            Assert.Equal(activities[0].Status, (string)jobj["data"]["activities"]["items"][0]["status"]);
            Assert.Equal(activities[0].StartDate, (DateTime)jobj["data"]["activities"]["items"][0]["startDate"]);
            Assert.Equal(activities[0].EndDate, (DateTime)jobj["data"]["activities"]["items"][0]["endDate"]);
        }

        [Fact]
        public async Task Query_Relations_By_Id_With_Variables_Returns_Relation()
        {
            // Arrange
            var relations = GetRelationDbos();
            await SaveRelationsToDb(relations);

            // Act
            var expectedRelation = relations[0];
            var variables = new
            {
                id = expectedRelation.EntityId
            };
            var query = @"
                query ($id: ID!) {
                    relations(ids:[$id]) {
                         items{
                           entityId
                           entityType
                        }
                    }
                }";

            using var response = await ClientQueryExecutor.ExecuteGet(_client, query, variables);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(relations[0].EntityId, (Guid)jobj["data"]["relations"]["items"][0]["entityId"]);
            Assert.Equal(relations[0].EntityType, (string)jobj["data"]["relations"]["items"][0]["entityType"]);
        }

        [Fact]
        public async Task Query_Processes_By_Id_With_Variables_Returns_Empty_Array()
        {
            // Act
            var nonExistentId = Guid.Empty;
            var variables = new
            {
                id = nonExistentId
            };
            var query = @"
                query ($id: ID!) {
                    processes(ids:[$id]) {
                        items{
                             operationId
                             workflowRunName
                             status
                             createdBy
                             createdDate
                             changedBy
                             changedDate
                        }
                    }
                }";

            using var response = await ClientQueryExecutor.ExecuteGet(_client, query, variables);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(jobj["data"]["processes"]["items"].ToArray());
        }

        [Fact]
        public async Task Query_Activities_By_Id_With_Variables_Returns_Empty_Array()
        {
            // Act
            var nonExistentId = Guid.Empty;
            var variables = new
            {
                id = nonExistentId
            };
            var query = @"
                query ($id: ID!) {
                    activities(ids:[$id]) {
                         items{
                             operationId
                             name
                             status
                             startDate
                             endDate
                        }
                    }
                }";

            using var response = await ClientQueryExecutor.ExecuteGet(_client, query, variables);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(jobj["data"]["activities"]["items"].ToArray());
        }

        [Fact]
        public async Task Query_Relations_By_Id_With_Variables_Returns_Empty_Array()
        {
            // Act
            var nonExistentId = Guid.Empty;
            var variables = new
            {
                id = nonExistentId
            };
            var query = @"
                query ($id: ID!) {
                     relations(ids:[$id]) {
                         items{
                           entityId
                           entityType
                        }
                    }
                }";

            using var response = await ClientQueryExecutor.ExecuteGet(_client, query, variables);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(jobj["data"]["relations"]["items"].ToArray());
        }

        [Fact]
        public async Task Query_Processes_By_Filter_Returns_Process()
        {
            // Arrange
            var processes = GetWorkflowRunDbos();
            await SaveProcessesToDb(processes);

            var expectedName = processes[0].WorkflowRunName;

            // Act
            var query = @$"
                {{
                    processes(
                        where: {{
                            path: ""workflowRunName"",
                            comparison: ""equal"",
                            value: ""{expectedName}""
                        }}
                    ){{
                         items{{
                             operationId
                             workflowRunName
                             status
                             createdBy
                             createdDate
                             changedBy
                             changedDate
                        }}
                    }}
                }}";

            using var response = await ClientQueryExecutor.ExecuteGet(_client, query);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(processes[0].OperationId, (Guid)jobj["data"]["processes"]["items"][0]["operationId"]);
            Assert.Equal(processes[0].WorkflowRunName, (string)jobj["data"]["processes"]["items"][0]["workflowRunName"]);
            Assert.Equal(processes[0].Status, (string)jobj["data"]["processes"]["items"][0]["status"]);
            Assert.Equal(processes[0].CreatedBy, (string)jobj["data"]["processes"]["items"][0]["createdBy"]);
            Assert.Equal(processes[0].ChangedBy, (string)jobj["data"]["processes"]["items"][0]["changedBy"]);
        }

        [Fact]
        public async Task Query_Activities_By_Filter_Returns_Activity()
        {
            // Arrange
            var activities = GetActivityDbos();
            await SaveActivitiesToDb(activities);

            var expectedName = activities[0].Name;

            // Act
            var query = @$"
                {{
                    activities(
                        where: {{
                            path: ""name"",
                            comparison: ""equal"",
                            value: ""{expectedName}""
                        }}
                    ){{
                         items{{
                             operationId
                             name
                             status
                             startDate
                             endDate
                        }}
                    }}
                }}";

            using var response = await ClientQueryExecutor.ExecuteGet(_client, query);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(activities[0].OperationId, (Guid)jobj["data"]["activities"]["items"][0]["operationId"]);
            Assert.Equal(activities[0].Name, (string)jobj["data"]["activities"]["items"][0]["name"]);
            Assert.Equal(activities[0].Status, (string)jobj["data"]["activities"]["items"][0]["status"]);
            Assert.Equal(activities[0].StartDate, (DateTime)jobj["data"]["activities"]["items"][0]["startDate"]);
            Assert.Equal(activities[0].EndDate, (DateTime)jobj["data"]["activities"]["items"][0]["endDate"]);
        }

        [Fact]
        public async Task Query_Relations_By_Filter_Returns_Relation()
        {
            // Arrange
            var relations = GetRelationDbos();
            await SaveRelationsToDb(relations);

            var expectedEntityId = relations[0].EntityId;

            // Act
            var query = @$"
                {{
                    relations(
                        where: {{
                            path: ""entityId"",
                            comparison: ""equal"",
                            value: ""{expectedEntityId}""
                        }}
                    ){{
                         items{{
                           entityId
                           entityType
                        }}
                    }}
                }}";

            using var response = await ClientQueryExecutor.ExecuteGet(_client, query);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(relations[0].EntityId, (Guid)jobj["data"]["relations"]["items"][0]["entityId"]);
            Assert.Equal(relations[0].EntityType, (string)jobj["data"]["relations"]["items"][0]["entityType"]);
        }

        [Fact]
        public async Task Query_Processes_Filter_By_Status_Returns_Processes()
        {
            // Arrange
            var processes = GetWorkflowRunDbos();
            await SaveProcessesToDb(processes);

            var expectedStatus = "failed";

            // Act
            var query = @$"
                {{
                    processes(
                        where: {{
                            path: ""status"",
                            comparison: ""equal"",
                            value: ""{expectedStatus}""
                        }}
                    ){{
                         items{{
                             operationId
                             workflowRunName
                             status
                             createdBy
                             createdDate
                             changedBy
                             changedDate
                        }}
                    }}
                }}";
            using var response = await ClientQueryExecutor.ExecuteGet(_client, query);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(processes[1].OperationId, (Guid)jobj["data"]["processes"]["items"][0]["operationId"]);
            Assert.Equal(processes[1].WorkflowRunName, (string)jobj["data"]["processes"]["items"][0]["workflowRunName"]);
            Assert.Equal(processes[1].Status, (string)jobj["data"]["processes"]["items"][0]["status"]);
            Assert.Equal(processes[1].CreatedBy, (string)jobj["data"]["processes"]["items"][0]["createdBy"]);
            Assert.Equal(processes[1].ChangedBy, (string)jobj["data"]["processes"]["items"][0]["changedBy"]);

            Assert.Equal(processes[2].OperationId, (Guid)jobj["data"]["processes"]["items"][1]["operationId"]);
            Assert.Equal(processes[2].WorkflowRunName, (string)jobj["data"]["processes"]["items"][1]["workflowRunName"]);
            Assert.Equal(processes[2].Status, (string)jobj["data"]["processes"]["items"][1]["status"]);
            Assert.Equal(processes[2].CreatedBy, (string)jobj["data"]["processes"]["items"][1]["createdBy"]);
            Assert.Equal(processes[2].ChangedBy, (string)jobj["data"]["processes"]["items"][1]["changedBy"]);
        }

        [Fact]
        public async Task Query_Activities_Filter_By_Status_Returns_Activities()
        {
            // Arrange
            var activities = GetActivityDbos();
            await SaveActivitiesToDb(activities);

            var expectedStatus = "Failed";

            // Act
            var query = @$"
                {{
                    activities(
                        where: {{
                            path: ""status"",
                            comparison: ""equal"",
                            value: ""{expectedStatus}""
                        }}
                    ){{
                         items{{
                             operationId
                             name
                             status
                             startDate
                             endDate
                        }}
                    }}
                }}";
            using var response = await ClientQueryExecutor.ExecuteGet(_client, query);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(activities[1].OperationId, (Guid)jobj["data"]["activities"]["items"][0]["operationId"]);
            Assert.Equal(activities[1].Name, (string)jobj["data"]["activities"]["items"][0]["name"]);
            Assert.Equal(activities[1].Status, (string)jobj["data"]["activities"]["items"][0]["status"]);
            Assert.Equal(activities[1].StartDate, (DateTime)jobj["data"]["activities"]["items"][0]["startDate"]);
            Assert.Equal(activities[1].EndDate, (DateTime)jobj["data"]["activities"]["items"][0]["endDate"]);

            Assert.Equal(activities[2].OperationId, (Guid)jobj["data"]["activities"]["items"][1]["operationId"]);
            Assert.Equal(activities[2].Name, (string)jobj["data"]["activities"]["items"][1]["name"]);
            Assert.Equal(activities[2].Status, (string)jobj["data"]["activities"]["items"][1]["status"]);
            Assert.Equal(activities[2].StartDate, (DateTime)jobj["data"]["activities"]["items"][1]["startDate"]);
            Assert.Equal(activities[2].EndDate, (DateTime)jobj["data"]["activities"]["items"][1]["endDate"]);
        }

        [Fact]
        public async Task Query_Relations_Filter_By_EntityType_Returns_Entities()
        {
            // Arrange
            var relations = GetRelationDbos();
            await SaveRelationsToDb(relations);

            var expectedEntityType = "Person";

            // Act
            var query = @$"
                {{
                    relations(
                        where: {{
                            path: ""entityType"",
                            comparison: ""equal"",
                            value: ""{expectedEntityType}""
                        }}
                    ){{
                         items{{
                           entityId
                           entityType
                        }}
                    }}
                }}";
            using var response = await ClientQueryExecutor.ExecuteGet(_client, query);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(relations[0].EntityId, (Guid)jobj["data"]["relations"]["items"][0]["entityId"]);
            Assert.Equal(relations[0].EntityType, (string)jobj["data"]["relations"]["items"][0]["entityType"]);

            Assert.Equal(relations[1].EntityId, (Guid)jobj["data"]["relations"]["items"][1]["entityId"]);
            Assert.Equal(relations[1].EntityType, (string)jobj["data"]["relations"]["items"][1]["entityType"]);
        }

        [Fact]
        public async Task Query_Processes_Paging()
        {
            // Arrange
            var processes = GetWorkflowRunDbos();
            await SaveProcessesToDb(processes);

            // Act
            var first = 2;
            var after = 0;
            var query = @$"
                {{
                    processes(first:{first}, after:""{after}"") {{
                        edges {{
                            cursor
                            node {{
                                 operationId
                                 workflowRunName
                                 status
                                 createdBy
                                 createdDate
                                 changedBy
                                 changedDate
                            }}
                        }}
                        pageInfo {{
                            endCursor
                            hasNextPage
                        }}
                    }}
                }}";
            using var response = await ClientQueryExecutor.ExecuteGet(_client, query);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(processes[1].OperationId, (Guid)jobj["data"]["processes"]["edges"][0]["node"]["operationId"]);
            Assert.Equal(processes[1].WorkflowRunName, (string)jobj["data"]["processes"]["edges"][0]["node"]["workflowRunName"]);
            Assert.Equal(processes[1].Status, (string)jobj["data"]["processes"]["edges"][0]["node"]["status"]);
            Assert.Equal(processes[1].CreatedBy, (string)jobj["data"]["processes"]["edges"][0]["node"]["createdBy"]);
            Assert.Equal(processes[1].ChangedBy, (string)jobj["data"]["processes"]["edges"][0]["node"]["changedBy"]);

            Assert.Equal(processes[2].OperationId, (Guid)jobj["data"]["processes"]["edges"][1]["node"]["operationId"]);
            Assert.Equal(processes[2].WorkflowRunName, (string)jobj["data"]["processes"]["edges"][1]["node"]["workflowRunName"]);
            Assert.Equal(processes[2].Status, (string)jobj["data"]["processes"]["edges"][1]["node"]["status"]);
            Assert.Equal(processes[2].CreatedBy, (string)jobj["data"]["processes"]["edges"][1]["node"]["createdBy"]);
            Assert.Equal(processes[2].ChangedBy, (string)jobj["data"]["processes"]["edges"][1]["node"]["changedBy"]);
        }

        [Fact]
        public async Task Query_Activities_Paging()
        {
            // Arrange
            var activities = GetActivityDbos();
            await SaveActivitiesToDb(activities);

            // Act
            var first = 2;
            var after = 0;
            var query = @$"
                {{
                    activities(first:{first}, after:""{after}"") {{
                        edges {{
                            cursor
                            node {{
                                 operationId
                                 name
                                 status
                                 startDate
                                 endDate
                            }}
                        }}
                        pageInfo {{
                            endCursor
                            hasNextPage
                        }}
                    }}
                }}";
            using var response = await ClientQueryExecutor.ExecuteGet(_client, query);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(activities[1].OperationId, (Guid)jobj["data"]["activities"]["edges"][0]["node"]["operationId"]);
            Assert.Equal(activities[1].Name, (string)jobj["data"]["activities"]["edges"][0]["node"]["name"]);
            Assert.Equal(activities[1].Status, (string)jobj["data"]["activities"]["edges"][0]["node"]["status"]);
            Assert.Equal(activities[1].StartDate, (DateTime)jobj["data"]["activities"]["edges"][0]["node"]["startDate"]);
            Assert.Equal(activities[1].EndDate, (DateTime)jobj["data"]["activities"]["edges"][0]["node"]["endDate"]);

            Assert.Equal(activities[2].OperationId, (Guid)jobj["data"]["activities"]["edges"][1]["node"]["operationId"]);
            Assert.Equal(activities[2].Name, (string)jobj["data"]["activities"]["edges"][1]["node"]["name"]);
            Assert.Equal(activities[2].Status, (string)jobj["data"]["activities"]["edges"][1]["node"]["status"]);
            Assert.Equal(activities[2].StartDate, (DateTime)jobj["data"]["activities"]["edges"][1]["node"]["startDate"]);
            Assert.Equal(activities[2].EndDate, (DateTime)jobj["data"]["activities"]["edges"][1]["node"]["endDate"]);
        }

        [Fact]
        public async Task Query_Relations_Paging()
        {
            // Arrange
            var relations = GetRelationDbos();
            await SaveRelationsToDb(relations);

            // Act
            var first = 2;
            var after = 0;
            var query = @$"
                {{
                    relations(first:{first}, after:""{after}"") {{
                        edges {{
                            cursor
                            node {{
                                 entityId
                                 entityType
                            }}
                        }}
                        pageInfo {{
                            endCursor
                            hasNextPage
                        }}
                    }}
                }}";
            using var response = await ClientQueryExecutor.ExecuteGet(_client, query);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(relations[1].EntityId, (Guid)jobj["data"]["relations"]["edges"][0]["node"]["entityId"]);
            Assert.Equal(relations[1].EntityType, (string)jobj["data"]["relations"]["edges"][0]["node"]["entityType"]);
        }

        private IReadOnlyList<WorkflowRunDbo> GetWorkflowRunDbos()
        {
            var dateTime = DateTime.UtcNow;
            const string createdByDefault = "createdBy1";
            const string changedByDefault = "changedBy1";

            var processWithRelations = new WorkflowRunDbo()
            {
                WorkflowRunName = "Name",
                OperationId = Guid.NewGuid(),
                Status = "in progress",
                ChangedBy = changedByDefault,
                ChangedDate = dateTime,
                CreatedBy = createdByDefault,
                CreatedDate = dateTime,
            };
            processWithRelations.WorkflowRelations = new List<WorkflowRelationDbo>
            {
                new WorkflowRelationDbo
                {
                    OperationId = processWithRelations.OperationId,
                    EntityId = Guid.NewGuid()
                }
            };

            var processList = new List<WorkflowRunDbo>
            {
                processWithRelations,
                new WorkflowRunDbo()
                {
                    WorkflowRunName = "Name2",
                    OperationId = Guid.NewGuid(),
                    Status = "failed",
                    ChangedBy = changedByDefault,
                    ChangedDate = dateTime,
                    CreatedBy = createdByDefault,
                    CreatedDate = dateTime,
                },
                new WorkflowRunDbo()
                {
                    WorkflowRunName = "Name3",
                    OperationId = Guid.NewGuid(),
                    Status = "failed",
                    ChangedBy = changedByDefault,
                    ChangedDate = dateTime,
                    CreatedBy = createdByDefault,
                    CreatedDate = dateTime,
                },
            };

            return processList;
        }

        private IReadOnlyList<ActivityDbo> GetActivityDbos()
        {
            var dateTime = DateTime.UtcNow;

            var activitiesList = new List<ActivityDbo>
            {
                new ActivityDbo
                {
                    ActivityId = Guid.NewGuid(),
                    Name = "Test",
                    Status = "Succeeded",
                    URI = "test/test",
                    StartDate = dateTime,
                    EndDate = dateTime,
                    OperationId = Guid.NewGuid()
                },
                new ActivityDbo
                {
                    ActivityId = Guid.NewGuid(),
                    Name = "Test2",
                    Status = "Failed",
                    URI = "test/test",
                    StartDate = dateTime,
                    EndDate = dateTime,
                    OperationId = Guid.NewGuid()
                },
                new ActivityDbo
                {
                    ActivityId = Guid.NewGuid(),
                    Name = "Test3",
                    Status = "Failed",
                    URI = "test/test",
                    StartDate = dateTime,
                    EndDate = dateTime,
                    OperationId = Guid.NewGuid()
                }
            };

            return activitiesList;
        }

        private IReadOnlyList<RelationDbo> GetRelationDbos()
        {
            var relationWithWorkflow = new RelationDbo()
            {
                EntityId = Guid.NewGuid(),
                EntityType = "Person",
                WorkflowRelations = new List<WorkflowRelationDbo>()
                {
                    new WorkflowRelationDbo()
                    {
                        WorkflowRun = new WorkflowRunDbo()
                        {
                            OperationId = Guid.NewGuid()
                        }
                    }
                }
            };

            var relationList = new List<RelationDbo>
            {
                relationWithWorkflow,
                new RelationDbo
                {
                    EntityId = Guid.NewGuid(),
                    EntityType = "Person"
                }
            };

            return relationList;
        }

        private async Task SaveProcessesToDb(IEnumerable<WorkflowRunDbo> processes)
        {
            var context = Resolve<ProcesManagerDbContext>();
            await context.WorkflowRuns.AddRangeAsync(processes);
            await context.SaveChangesAsync();
        }

        private async Task SaveActivitiesToDb(IEnumerable<ActivityDbo> activities)
        {
            var context = Resolve<ProcesManagerDbContext>();
            await context.Activities.AddRangeAsync(activities);
            await context.SaveChangesAsync();
        }

        private async Task SaveRelationsToDb(IEnumerable<RelationDbo> relations)
        {
            var context = Resolve<ProcesManagerDbContext>();
            await context.Relations.AddRangeAsync(relations);
            await context.SaveChangesAsync();
        }

        private TResult Resolve<TResult>()
        {
            return _host.Services.GetAutofacRoot().Resolve<TResult>();
        }
    }
}
