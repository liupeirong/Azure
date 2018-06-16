package com.pliu.powerbiembed;

public class EmbedToken {
    public String token;
    public String tokenId;
    public String expiration;

    public void setToken (String token) {this.token = token;}
    public void setTokenId (String tokenId) {this.tokenId = tokenId;}
    public void setExpiration (String expiration) {this.expiration = expiration;}

    public String getToken () {return this.token;}
    public String getTokenId() {return this.tokenId;}
    public String getExpiration() {return this.expiration;}
}
