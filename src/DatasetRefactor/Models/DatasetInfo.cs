﻿using System;

namespace DatasetRefactor.Models
{
    public class DatasetInfo
    {
        public DatasetInfo()
        {
        }

        public DatasetInfo(Type type)
        {
            this.Name = type.Name;
            this.Namespace = type.Namespace;
        }

        public string Name { get; set; }

        public string Namespace { get; set; }
    }
}
