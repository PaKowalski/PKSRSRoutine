using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Loki.Bot;
using Loki.Common;

namespace PKSRSRoutine
{
    public class PKSRSRoutine : IRoutine
    {
        public UserControl Control { get; }
        public JsonSettings Settings { get; }
        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Tick()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public string Name { get; }
        public string Author { get; }
        public string Description { get; }
        public string Version { get; }
        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void Deinitialize()
        {
            throw new NotImplementedException();
        }

        public Task<bool> Logic(string type, params dynamic[] param)
        {
            throw new NotImplementedException();
        }

        public object Execute(string name, params dynamic[] param)
        {
            throw new NotImplementedException();
        }
    }
}
