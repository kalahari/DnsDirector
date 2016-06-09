using ARSoft.Tools.Net.Dns;
using log4net;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DnsDirector.Service
{
    class Server : DnsServer
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Server));
        private readonly Router router;

        public Server(Router router) : base(IPAddress.Loopback, 12, 1)
        {
            log.Debug("new Server()");
            this.router = router;
            QueryReceived += Server_QueryReceived;
        }
        
        private Task Server_QueryReceived(object sender, QueryReceivedEventArgs eventArgs)
        {
            log.Debug("Server_QueryReceived()");
            using (LogicalThreadContext.Stacks["NDC"].Push(eventArgs.Query.TransactionID.ToString()))
            {
                try {
                    return ProcessQuery(eventArgs);
                }
                catch(Exception ex)
                {
                    log.Error($"Error processing query", ex);
                    // shold this error be swallowed here?
                    return Task.Run(() => { throw ex; });
                }
            }
        }

        private async Task ProcessQuery(QueryReceivedEventArgs eventArgs)
        {
            var message = eventArgs.Query as DnsMessage;
            log.Debug($"Processing query");

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

        private async Task QueryUpstream(DnsQuestion question, DnsMessage response)
        {
            using (LogicalThreadContext.Stacks["NDC"].Push(question.ToString()))
            {
                // send query to upstream server
                log.Debug($"Questioning: {question.Name}");
                var resolvers = router.GetResolvers(question.Name.ToString());
                bool answered = false;
                var client = new DnsClient(resolvers, 10000);
                var upstreamResponse = await client.ResolveAsync(question.Name, question.RecordType, question.RecordClass);

                // if got an answer, copy it to the message sent to the client
                log.Debug($"Received a response for query, previously answered: {answered}");
                if (upstreamResponse != null && !answered)
                {
                    answered = true;
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
