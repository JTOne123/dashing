﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopHat.Configuration
{
    public class DefaultConfiguration : Configuration
    {
        public override Mapper.Mapper Configure()
        {
            this.AlwaysTrackEntities = false;
            this.PrimaryKeysDatabaseGeneratedByDefault = true;
            this.GenerateIndexesOnForeignKeysByDefault = true;

            return this.mapper;
        }
    }
}