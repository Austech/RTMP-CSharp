﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiscUtil.IO;
using MiscUtil.Conversion;

namespace RTMP
{
    public class AmfData
    {
        public List<AmfObject> Objects;
        public List<string> Strings;
        public List<double> Numbers;
        public List<bool> Booleans;
        public uint Nulls;
        public AmfData()
        {
            Objects = new List<AmfObject>();
            Strings = new List<string>();
            Numbers = new List<double>();
            Booleans = new List<bool>();
            Nulls = 0;
        }
    }
    public class AmfReader
    {
        public AmfData amfData;
 
        public AmfReader()
        {
            amfData = new AmfData();
        }

        public void Parse(EndianBinaryReader reader, uint size)
        {
            var maxReadPos = reader.BaseStream.Position + size;
            while(reader.BaseStream.Position < maxReadPos)
            {
                var type = (Amf0Types)reader.ReadByte();
                switch (type)
                {
                    case Amf0Types.Object:
                        {
                            bool hasProperty = false;
                            string property = "";
                            var objectAdd = new AmfObject();
                            while (reader.BaseStream.Position < maxReadPos)
                            {
                                if(!hasProperty)
                                {
                                    property = "";
                                    var propertyStringLength = reader.ReadUInt16();
                                    for (var i = 0; i < propertyStringLength; i++)
                                    {
                                        property += (char)reader.ReadByte();
                                    }
                                    hasProperty = true;
                                }

                                if (hasProperty == true)
                                {
                                    if(property.Length == 0)
                                    {
                                        amfData.Objects.Add(objectAdd);

                                        break;
                                    }
                                    var objtype = (Amf0Types)reader.ReadByte();
                                    parseType(objtype, reader, ref objectAdd.Nulls, objectNumbers: objectAdd.Numbers,
                                              objectBooleans: objectAdd.Booleans, objectStrings: objectAdd.Strings,
                                              property: property);
                                    hasProperty = false;
                                }
                            }
                        }
                        break;
                    default:
                        parseType(type, reader, ref amfData.Nulls, amfData.Numbers, amfData.Booleans, amfData.Strings);
                        break;
                }
            }
        }

        //jesus fuck note to self: fix this
        private static void parseType(Amf0Types type, EndianBinaryReader reader,
            ref uint Nulls,
            List<double> Numbers = null,
            List<bool> Booleans = null,
            List<string> Strings = null,
            Dictionary<string, string> objectStrings = null,
            Dictionary<string, double> objectNumbers = null,
            Dictionary<string, bool> objectBooleans = null,
            string property = "")
        {
            switch (type)
            {
                case Amf0Types.Number:
                    if (Numbers != null)
                        Numbers.Add(reader.ReadDouble());
                    else
                        objectNumbers.Add(property, reader.ReadDouble());
                    break;
                case Amf0Types.Boolean:
                    if (Booleans != null)
                        Booleans.Add(reader.ReadBoolean());
                    else
                        objectBooleans.Add(property, reader.ReadBoolean());
                    break;
                case Amf0Types.String:
                    {
                        var count = reader.ReadUInt16();
                        var pushString = "";
                        for (var i = 0; i < count; i++)
                        {
                            pushString += (char) reader.ReadByte();
                        }
                        if (Strings != null)
                            Strings.Add(pushString);
                        else
                            objectStrings.Add(property, pushString);
                    }
                    break;
                case Amf0Types.Null:
                    Nulls++;
                    break;
                case Amf0Types.Array:
                    break;
                case Amf0Types.ObjectEnd:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}