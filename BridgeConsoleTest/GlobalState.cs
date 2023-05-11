using NLog;
using ResourceCollection;
using System;
using System.Collections.Generic;
using XiliumXWT;

namespace BridgeConsole
{
    internal class GlobalState : IInternalHttpRequestHandler, IExternalHttpRequestHandler, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly object _lockContext = new object();

        private SharedState _commonState = new SharedState();

        public readonly ConsoleController ConsoleController;


        public GlobalState()
        {
            ConsoleController = new ConsoleController(_commonState);
        }



        public byte[] HandleHttpRequest(string collection, IDictionary<string, string> evt)
        {
            try
            {
                switch (collection)
                {

                    case "enginestate":
                        return ConsoleController.HandleHttpRequest(collection, evt);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }


            return null;
        }

        public byte[] HandleHttpProtobufRequest(string collectionname, byte[] data)
        {
            try
            {

            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            return null;
        }



        public object LockContext() => _lockContext;




        public void Dispose()
        {

        }
    }
}