//*********************************************************
//
//    Copyright (c) Microsoft. All rights reserved.
//    This code is licensed under the Microsoft Public License.
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using FileHelpers;

namespace geoLogGen
{
    public class PlayerPool
    {
        public Guid[] players;
        public int numPlayers;
        public PlayerPool (int cPlayers)
        {
            players = new Guid[cPlayers];
            for (int ii = 0; ii < cPlayers; ++ii)
                players[ii] = Guid.NewGuid();
            numPlayers = cPlayers;
        }
    }
    
    [DelimitedRecord(","), IgnoreFirst]
    public class City : IComparable
    {
        public string name;
        public double lat;
        public double lon;
        public string country;

        public int CompareTo (object obj)
        {
            // sort east (+180) to west (-180)
            // if obj.lon > this.lon, obj is east of this
            // Shanghai 121, NY -73
            City toCity = obj as City;
            return (int)(toCity.lon - lon);
        }
    }

    public class LocationPool
    {
        public City[] cities;
        public int numCities;
        public LocationPool(string filePath)
        {
            var engine = new FileHelperEngine<City>();
            cities = engine.ReadFile(filePath);
            Array.Sort(cities);
            numCities = cities.Length;
        }
    }

    public class Session
    {
        public enum eventTypeList { gameStart = 0, levelReached, itemPurchased, gameEnd };
        public enum purchaseItemList { crystal = 0, medicine, sword, pet };
        public int[] itemPrice = { 23, 17, 65, 121 };
        public enum levelList { beginner = 0, casual, medium, master };

        public Guid playerId;
        public Guid sessionId;
        public eventTypeList eventType;
        public levelList level;
        public DateTimeOffset eventTime;
        public String eventParams;

        Random rand;
        LocationPool cities;
        int numCities;
        int citiesInChunk;

        public Session(Random coin, PlayerPool players, LocationPool locations)
        {
            rand = coin;
            sessionId = Guid.NewGuid();

            int playerIdx = rand.Next(0, players.numPlayers - 1);
            playerId = players.players[playerIdx];

            level = (levelList)rand.Next(0, (int)levelList.master);

            cities = locations;
            numCities = cities.numCities;
            citiesInChunk = numCities / 60; //rotate the world in 60 minutes
        }

        public void setEvent(eventTypeList type, DateTimeOffset time)
        {
            eventType = type;
            eventTime = time;
            switch (type)
            {
                case eventTypeList.gameStart:
                    generateGameStartParams();
                    break;
                case eventTypeList.levelReached:
                    generateLevelReachedParams();
                    break;
                case eventTypeList.itemPurchased:
                    generateItemPurchasedParams();
                    break;
            }
        }

        private void generateGameStartParams()
        {
            // rotate the earth in 60 minutes, cities are sorted east to west
            // divide cities in 60 chuncks
            // each minute of the hour find a random city in that chunk
            int curMin = eventTime.Minute;

            int cityIdx = rand.Next(curMin * citiesInChunk, curMin < 50 - 1 ? (curMin + 1) * citiesInChunk : numCities );
            City city = cities.cities[cityIdx];
            eventParams = String.Format("{{\"city\":\"{0}\",\"lat\":{1},\"lon\":{2}}}", city.name, city.lat, city.lon);
        }

        private void generateLevelReachedParams()
        {
            if (level < levelList.master) ++level;
            eventParams = String.Format("{{\"level\":\"{0}\"}}", level);
        }

        private void generateItemPurchasedParams()
        {
            int itemIdx = rand.Next(0, (int)purchaseItemList.pet + 1);
            purchaseItemList item = (purchaseItemList)itemIdx;
            int price = itemPrice[itemIdx];
            int quantity = rand.Next(1, 10);

            eventParams = String.Format("{{\"item\":\"{0}\",\"quantity\":{1},\"price\":{2}}}", item.ToString(), quantity, price);
        }
    }
}
