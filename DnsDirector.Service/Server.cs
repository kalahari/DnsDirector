using ARSoft.Tools.Net.Dns;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DnsDirector.Service
{
    class Server : DnsServer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Server));

        public Server() : base(IPAddress.Loopback, 1, 1)
        {
            QueryReceived += Server_QueryReceived;
        }

        private Task Server_QueryReceived(object sender, QueryReceivedEventArgs eventArgs)
        {
            using (log4net.LogicalThreadContext.Stacks["NDC"].Push(eventArgs.Query.TransactionID.ToString()))
            {
                return ProcessQuery(eventArgs);
            }
        }

        private async Task ProcessQuery(QueryReceivedEventArgs eventArgs)
        {
            var message = eventArgs.Query as DnsMessage;

            // nothing to respond to
            if (message == null)
            {
                log.Warn("Query is null!");
                return;
            }

            var response = message.CreateResponseInstance();

            if (!message.Questions.Any())
            {
                response.ReturnCode = ReturnCode.ServerFailure;
            }
            else
            {
                var upstreams = message.Questions.Select(q => QueryUpstream(q, response));
                await Task.WhenAll(upstreams);

                response.ReturnCode = ReturnCode.NoError;
            }

            // set the response
            eventArgs.Response = response;
        }

        private static async Task QueryUpstream(DnsQuestion question, DnsMessage response)
        {
            using (log4net.LogicalThreadContext.Stacks["NDC"].Push(question.ToString()))
            {
                // send query to upstream server
                log.Debug($"Questioning: {question.Name}");
                var upstreamResponse = await DnsClient.Default.ResolveAsync(question.Name, question.RecordType, question.RecordClass);

                // if got an answer, copy it to the message sent to the client
                if (upstreamResponse != null)
                {
                    foreach (var record in (upstreamResponse.AnswerRecords))
                    {
                        log.Debug($"Answer: {record.ToString()}");
                        response.AnswerRecords.Add(record);
                    }
                    foreach (var record in (upstreamResponse.AdditionalRecords))
                    {
                        log.Debug($"Additional: {record.ToString()}");
                        response.AdditionalRecords.Add(record);
                    }
                }
            }
        }
    }
}
