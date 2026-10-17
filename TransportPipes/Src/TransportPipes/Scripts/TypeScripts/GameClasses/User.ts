class User {


    constructor() {
        this.userResourceHandler = new UserResourceHandler();
    }

    userResourceHandler: UserResourceHandler;

    userId: number;
    userName: string;
    loggedIn: boolean;
} 