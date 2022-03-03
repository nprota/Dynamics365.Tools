using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using PZone.Xrm.Workflow;
using System;
using System.Activities;
using System.Linq;

namespace PZone.DateTools.Workflow
{
    public class UserDate : WorkflowBase
    {
        [RequiredArgument]
        [Input("Дата время")]
        public InArgument<DateTime> InputDateTime { get; set; }

        [Input("Пользователь")]
        [ReferenceTarget("systemuser")]
        public InArgument<EntityReference> User { get; set; }

        [Output("Пользовательские дата и время")]
        public OutArgument<DateTime> OutputDate { get; set; }

        protected override void Execute(Context context)
        {
            var inputDate = InputDateTime.Get(context);
            var userRef = User.Get(context) ?? new EntityReference("systemuser", context.SourceContext.UserId);
            var result = GetLocalDateTime(inputDate, context.Service, userRef.Id);
            OutputDate.Set(context, result);
        }

        private DateTime GetLocalDateTime(DateTime input, IOrganizationService service, Guid userId)
        {
            var userTimeZone = service.RetrieveMultiple(new FetchExpression($@"
                <fetch no-lock='true' top='1'>
	                <entity name='usersettings' >
		                <attribute name='timezonecode'/>
		                <filter type='and'>
			                <condition attribute='systemuserid' operator='eq' value='{userId}' />
		                </filter>
                    </entity>
                </fetch>")).Entities.FirstOrDefault()?.GetAttributeValue<int?>("timezonecode");

            if (userTimeZone == null)
            {
                return input;
            }

            var request = new LocalTimeFromUtcTimeRequest
            {
                TimeZoneCode = userTimeZone.Value,
                UtcTime = input.ToUniversalTime()
            };

            var response = (LocalTimeFromUtcTimeResponse)service.Execute(request);
            return response.LocalTime;
        }
    }
}
